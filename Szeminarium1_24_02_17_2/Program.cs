using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using Szeminarium1_24_03_05_2;

namespace Szeminarium1_24_02_17_2
{
    internal static class Program
    {
        private static CameraDescriptor cameraDescriptor = new();

        private static CubeArrangementModel cubeArrangementModel = new();

        private static IWindow window;

        private static IInputContext inputContext;

        private static GL Gl;

        private static ImGuiController controller;

        private static uint program;

        private static GlObject blimp;

        private static GlObject table;

        private static GlCube glCubeRotating;

        private static GlCube skyBox;

        private static float Shininess = 50;
        private static float skyboxSize = 20f;
        private static List<float> coinRotationSpeeds = new List<float>();
        private static List<float> coinCurrentRotations = new List<float>();

        private static bool gameOver = false;
        private static string gameOverMessage = "";
        private static float gameOverTimer = 0f;
        private static List<float> mountainCollisionRadii = new List<float>();

        private static float closestMountainDistance = float.MaxValue;
        private static int closestMountainIndex = -1;
        private static bool showCollisionDebug = true;

        private static List<GlObject> coins = new List<GlObject>();
        private static List<Vector3D<float>> coinPositions = new List<Vector3D<float>>();
        private static List<bool> coinCollected = new List<bool>();
        private static List<Matrix4X4<float>> coinModelMatrices = new List<Matrix4X4<float>>();
        private static int score = 0;
        private static float coinCollectionRadius = 70f;

        private const string ModelMatrixVariableName = "uModel";
        private const string NormalMatrixVariableName = "uNormal";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";

        private const string TextureUniformVariableName = "uTexture";

        private const string LightColorVariableName = "lightColor";
        private const string LightPositionVariableName = "lightPos";
        private const string ViewPosVariableName = "viewPos";
        private const string ShininessVariableName = "shininess";

        private static List<GlObject> mountains = new List<GlObject>();
        private static List<Vector3D<float>> mountainPositions = new List<Vector3D<float>>();
        private static Random random = new Random();

        private static List<float> mountainScales = new List<float>();
        private static List<Matrix4X4<float>> mountainModelMatrices = new List<Matrix4X4<float>>();

        private static Vector3D<float> _blimpPosition = Vector3D<float>.Zero;
        private static List<GlObject> birds = new List<GlObject>();
        private static List<Vector3D<float>> birdPositions = new List<Vector3D<float>>();
        private static List<Vector3D<float>> birdStartPositions = new List<Vector3D<float>>();
        private static List<Matrix4X4<float>> birdModelMatrices = new List<Matrix4X4<float>>();
        private static List<float> birdFlightProgress = new List<float>();
        private static List<float> birdFlightSpeeds = new List<float>();
        private static List<int> birdFlightPatterns = new List<int>();
        private static List<float> birdRotations = new List<float>();
        private static float birdCollisionRadius = 80f;

        private static float birdFlightRadius = 2000f;
        private static float birdFlightHeight = 100f;
        private static float birdHeightVariation = 100f;
        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "2 szeminárium";
            windowOptions.Size = new Vector2D<int>(1000, 1000);

            windowOptions.PreferredDepthBufferBits = 24;

            window = Window.Create(windowOptions);

            window.Load += Window_Load;
            window.Update += Window_Update;
            window.Render += Window_Render;
            window.Closing += Window_Closing;

            window.Run();
        }

        private static void Window_Load()
        {
            //Console.WriteLine("Load");

            // set up input handling
            inputContext = window.CreateInput();
            foreach (var keyboard in inputContext.Keyboards)
            {
                keyboard.KeyDown += Keyboard_KeyDown;
            }

            Gl = window.CreateOpenGL();

            controller = new ImGuiController(Gl, window, inputContext);

            // Handle resizes
            window.FramebufferResize += s =>
            {
                // Adjust the viewport to the new window size
                Gl.Viewport(s);
            };


            Gl.ClearColor(System.Drawing.Color.Black);

            SetUpObjects();

            LinkProgram();

            Gl.Enable(EnableCap.CullFace);
            //Gl.Disable(GLEnum.CullFace);
            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);
        }

        private static void LinkProgram()
        {
            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(vshader, ReadShader("VertexShader.vert"));
            Gl.CompileShader(vshader);
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader));

            Gl.ShaderSource(fshader, ReadShader("FragmentShader.frag"));
            Gl.CompileShader(fshader);

            program = Gl.CreateProgram();
            Gl.AttachShader(program, vshader);
            Gl.AttachShader(program, fshader);
            Gl.LinkProgram(program);
            Gl.GetProgram(program, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                Console.WriteLine($"Error linking shader {Gl.GetProgramInfoLog(program)}");
            }
            Gl.DetachShader(program, vshader);
            Gl.DetachShader(program, fshader);
            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);
        }

        private static string ReadShader(string shaderFileName)
        {
            using (Stream shaderStream = typeof(Program).Assembly.GetManifestResourceStream("Szeminarium1_24_02_17_2.Shaders." + shaderFileName))
            using (StreamReader shaderReader = new StreamReader(shaderStream))
                return shaderReader.ReadToEnd();
        }

        private static void Keyboard_KeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            switch (key)
            {
                case Key.Space:
                    if (gameOver)
                    {
                        ResetGame();
                    }
                    else
                    {
                        cubeArrangementModel.AnimationEnabeld = !cubeArrangementModel.AnimationEnabeld;
                    }
                    break;
                case Key.R:
                    if (gameOver)
                    {
                        ResetGame();
                    }
                    break;
                case Key.F1:
                    showCollisionDebug = !showCollisionDebug;
                    break;
            }
        }

        private static void Window_Update(double deltaTime)
        {
            if (!gameOver)
            {
                foreach (var keyboard in inputContext.Keyboards)
                {
                    cameraDescriptor.ProcessInput(deltaTime, keyboard, ref _blimpPosition);
                }

                cameraDescriptor.Update(deltaTime, _blimpPosition);

                float blimpRadius = (float)cubeArrangementModel.CenterCubeScale * 2f;

                if (CheckCollisionWithMountains(_blimpPosition, blimpRadius))
                {
                    gameOver = true;
                    gameOverMessage = $"GAME OVER - Blimp crashed into mountain #{closestMountainIndex}!";
                    gameOverTimer = 0f;
                    Console.WriteLine($"{gameOverMessage} Distance was: {closestMountainDistance:F2}");
                    
                }

                if (CheckBirdCollisions(_blimpPosition, blimpRadius))
                {
                    gameOver = true;
                    gameOverMessage = "GAME OVER - Blimp hit by a bird!";
                    gameOverTimer = 0f;
                }

                CheckCoinCollection(_blimpPosition, blimpRadius);
                UpdateCoinRotations(deltaTime);
                UpdateBirds(deltaTime);

                cubeArrangementModel.AdvanceTime(deltaTime);
            }
            else
            {
                gameOverTimer += (float)deltaTime;
            }

            controller.Update((float)deltaTime);
        }
        private static void UpdateCoinRotations(double deltaTime)
        {
            for (int i = 0; i < coinCurrentRotations.Count; i++)
            {
                if (!coinCollected[i])
                {
                    coinCurrentRotations[i] += coinRotationSpeeds[i] * (float)deltaTime;

                    if (coinCurrentRotations[i] > Math.PI * 2)
                        coinCurrentRotations[i] -= (float)(Math.PI * 2);

                    float coinScale = 250f;
                    var scale = Matrix4X4.CreateScale(coinScale);
                    var rotation = Matrix4X4.CreateRotationY(coinCurrentRotations[i]);
                    var translation = Matrix4X4.CreateTranslation(coinPositions[i]);
                    coinModelMatrices[i] = scale * rotation * translation;
                }
            }
        }

        private static unsafe void DrawBlimp()
        {
            var scale = Matrix4X4.CreateScale((float)cubeArrangementModel.CenterCubeScale * 2f);
            var rotation = Matrix4X4.CreateRotationY(cameraDescriptor.Yaw);
            var translation = Matrix4X4.CreateTranslation(_blimpPosition);

            var modelMatrix = scale * rotation * translation;

            SetModelMatrix(modelMatrix);
            Gl.BindVertexArray(blimp.Vao);
            Gl.DrawElements(GLEnum.Triangles, blimp.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindVertexArray(0);
        }

        private static unsafe void DrawCoins()
        {
            for (int i = 0; i < coins.Count && i < coinModelMatrices.Count; i++)
            {
                if (!coinCollected[i])
                {
                    SetModelMatrix(coinModelMatrices[i]);
                    Gl.BindVertexArray(coins[i].Vao);
                    Gl.DrawElements(GLEnum.Triangles, coins[i].IndexArrayLength, GLEnum.UnsignedInt, null);
                    Gl.BindVertexArray(0);
                }
            }
        }

        private static unsafe void Window_Render(double deltaTime)
        {
            //Console.WriteLine($"Render after {deltaTime} [s].");

            // GL here
            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Clear(ClearBufferMask.DepthBufferBit);


            Gl.UseProgram(program);

            SetViewMatrix();
            SetProjectionMatrix();

            SetLightColor();
            SetLightPosition();
            SetViewerPosition();
            SetShininess();

            DrawBlimp();

            DrawMountains();

            DrawCoins();
            DrawBirds();

            DrawSkyBox();

            //ImGuiNET.ImGui.ShowDemoWindow();
            ImGuiNET.ImGui.Begin("Lighting properties",
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar);
            ImGuiNET.ImGui.SliderFloat("Shininess", ref Shininess, 1, 200);
            ImGuiNET.ImGui.End();

            if (gameOver)
            {
                // Create a centered window for game over message
                var io = ImGuiNET.ImGui.GetIO();
                ImGuiNET.ImGui.SetNextWindowPos(new System.Numerics.Vector2(io.DisplaySize.X * 0.5f, io.DisplaySize.Y * 0.5f),
                    ImGuiCond.Always, new System.Numerics.Vector2(0.5f, 0.5f));

                ImGuiNET.ImGui.Begin("Game Over",
                    ImGuiWindowFlags.AlwaysAutoResize |
                    ImGuiWindowFlags.NoMove |
                    ImGuiWindowFlags.NoResize |
                    ImGuiWindowFlags.NoCollapse);

                ImGuiNET.ImGui.Text(gameOverMessage);
                ImGuiNET.ImGui.Separator();
                ImGuiNET.ImGui.Text($"Final Score: {score} coins collected");
                ImGuiNET.ImGui.Text($"Survival Time: {gameOverTimer:F1} seconds");
                ImGuiNET.ImGui.Separator();
                ImGuiNET.ImGui.Text("Press SPACE or R to restart");

                if (ImGuiNET.ImGui.Button("Restart Game"))
                {
                    ResetGame();
                }

                ImGuiNET.ImGui.End();
            }
            else
            {
                ImGuiNET.ImGui.Begin("Game Status", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar);
                ImGuiNET.ImGui.Text($"Score: {score} coins");
                ImGuiNET.ImGui.Separator();
                ImGuiNET.ImGui.Text($"Blimp Position: ({_blimpPosition.X:F1}, {_blimpPosition.Y:F1}, {_blimpPosition.Z:F1})");

                float blimpRadius = (float)cubeArrangementModel.CenterCubeScale * 2f;
                ImGuiNET.ImGui.Text($"Blimp Radius: {blimpRadius:F1}");

                if (showCollisionDebug)
                {
                    ImGuiNET.ImGui.Separator();
                    ImGuiNET.ImGui.Text("Collision Debug Info:");
                    ImGuiNET.ImGui.Text($"Closest Mountain: #{closestMountainIndex}");
                    ImGuiNET.ImGui.Text($"Distance: {closestMountainDistance:F1}");

                    if (closestMountainIndex >= 0 && closestMountainIndex < mountainCollisionRadii.Count)
                    {
                        float mountainRadius = mountainCollisionRadii[closestMountainIndex];
                        float requiredDistance = 1065f;
                        ImGuiNET.ImGui.Text($"Mountain Radius: {mountainRadius:F1}");
                        ImGuiNET.ImGui.Text($"Required Distance: {requiredDistance:F1}");
                        ImGuiNET.ImGui.Text($"Collision: {(closestMountainDistance < requiredDistance ? "YES" : "NO")}");
                    }

                    ImGuiNET.ImGui.Text("Press F1 to toggle debug info");
                }

                ImGuiNET.ImGui.Text("Collect coins and avoid the mountains!");
                ImGuiNET.ImGui.End();
            }

            controller.Render();
        }
        private static unsafe void DrawMountains()
        {
            for (int i = 0; i < mountains.Count && i < mountainModelMatrices.Count; i++)
            {
                SetModelMatrix(mountainModelMatrices[i]);
                Gl.BindVertexArray(mountains[i].Vao);
                Gl.DrawElements(GLEnum.Triangles, mountains[i].IndexArrayLength, GLEnum.UnsignedInt, null);
                Gl.BindVertexArray(0);
            }
        }
        private static unsafe void DrawSkyBox()
        {
            Matrix4X4<float> modelMatrix = Matrix4X4.CreateScale(400f);
            SetModelMatrix(modelMatrix);
            Gl.BindVertexArray(skyBox.Vao);

            int textureLocation = Gl.GetUniformLocation(program, TextureUniformVariableName);
            if (textureLocation == -1)
            {
                throw new Exception($"{TextureUniformVariableName} uniform not found on shader.");
            }
            // set texture 0
            Gl.Uniform1(textureLocation, 0);

            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)GLEnum.Linear);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)GLEnum.Linear);
            Gl.BindTexture(TextureTarget.Texture2D, skyBox.Texture.Value);

            Gl.DrawElements(GLEnum.Triangles, skyBox.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindVertexArray(0);

            CheckError();
            Gl.BindTexture(TextureTarget.Texture2D, 0);
            CheckError();
        }

        private static unsafe void SetLightColor()
        {
            int location = Gl.GetUniformLocation(program, LightColorVariableName);

            if (location == -1)
            {
                throw new Exception($"{LightColorVariableName} uniform not found on shader.");
            }

            Gl.Uniform3(location, 1f, 1f, 1f);
            CheckError();
        }

        private static unsafe void SetLightPosition()
        {
            int location = Gl.GetUniformLocation(program, LightPositionVariableName);

            if (location == -1)
            {
                throw new Exception($"{LightPositionVariableName} uniform not found on shader.");
            }

            Gl.Uniform3(location, 0f, 10f, 0f);
            CheckError();
        }

        private static unsafe void SetViewerPosition()
        {
            int location = Gl.GetUniformLocation(program, ViewPosVariableName);

            if (location == -1)
            {
                throw new Exception($"{ViewPosVariableName} uniform not found on shader.");
            }

            Gl.Uniform3(location, cameraDescriptor.Position.X, cameraDescriptor.Position.Y, cameraDescriptor.Position.Z);
            CheckError();
        }

        private static unsafe void SetShininess()
        {
            int location = Gl.GetUniformLocation(program, ShininessVariableName);

            if (location == -1)
            {
                throw new Exception($"{ShininessVariableName} uniform not found on shader.");
            }

            Gl.Uniform1(location, Shininess);
            CheckError();
        }


        private static unsafe void SetModelMatrix(Matrix4X4<float> modelMatrix)
        {
            int location = Gl.GetUniformLocation(program, ModelMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{ModelMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&modelMatrix);
            CheckError();

            var modelMatrixWithoutTranslation = new Matrix4X4<float>(modelMatrix.Row1, modelMatrix.Row2, modelMatrix.Row3, modelMatrix.Row4);
            modelMatrixWithoutTranslation.M41 = 0;
            modelMatrixWithoutTranslation.M42 = 0;
            modelMatrixWithoutTranslation.M43 = 0;
            modelMatrixWithoutTranslation.M44 = 1;

            Matrix4X4<float> modelInvers;
            Matrix4X4.Invert<float>(modelMatrixWithoutTranslation, out modelInvers);
            Matrix3X3<float> normalMatrix = new Matrix3X3<float>(Matrix4X4.Transpose(modelInvers));
            location = Gl.GetUniformLocation(program, NormalMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{NormalMatrixVariableName} uniform not found on shader.");
            }
            Gl.UniformMatrix3(location, 1, false, (float*)&normalMatrix);
            CheckError();
        }

        private static void CreateBirds()
        {
            int birdCount = 15;

            birds.Clear();
            birdPositions.Clear();
            birdStartPositions.Clear();
            birdModelMatrices.Clear();
            birdFlightProgress.Clear();
            birdFlightSpeeds.Clear();
            birdFlightPatterns.Clear();
            birdRotations.Clear();

            Console.WriteLine($"Creating {birdCount} birds");

            for (int i = 0; i < birdCount; i++)
            {
                int flightPattern = i % 4;

                float startAngle = (float)(random.NextDouble() * Math.PI * 2);
                float startRadius = birdFlightRadius + (float)(random.NextDouble() - 0.5) * 500f;
                float startHeight = birdFlightHeight + (float)(random.NextDouble() - 0.5) * birdHeightVariation;

                Vector3D<float> startPosition = new Vector3D<float>(
                    (float)(Math.Cos(startAngle) * startRadius),
                    startHeight,
                    (float)(Math.Sin(startAngle) * startRadius)
                );

                float flightSpeed = 0.1f + (float)(random.NextDouble() * 0.1f);

                float startProgress = (float)random.NextDouble();

                Vector3D<float> initialPosition = CalculateBirdPosition(startPosition, flightPattern, startProgress);

                float birdScale =1f;
                var scale = Matrix4X4.CreateScale(birdScale);
                var translation = Matrix4X4.CreateTranslation(initialPosition);
                var modelMatrix = scale * translation;

                var bird = ObjectResourceReader.CreateObjectFromResource(Gl, "bird.obj");

                birds.Add(bird);
                birdPositions.Add(initialPosition);
                birdStartPositions.Add(startPosition);
                birdModelMatrices.Add(modelMatrix);
                birdFlightProgress.Add(startProgress);
                birdFlightSpeeds.Add(flightSpeed);
                birdFlightPatterns.Add(flightPattern);
                birdRotations.Add(0f);

                Console.WriteLine($"Bird {i}: Pattern {flightPattern}, Start Position({startPosition.X:F1}, {startPosition.Y:F1}, {startPosition.Z:F1})");
            }

            Console.WriteLine($"Successfully created {birds.Count} birds");
        }
        private static Vector3D<float> CalculateBirdPosition(Vector3D<float> startPos, int pattern, float progress)
        {
            Vector3D<float> position = startPos;

            switch (pattern)
            {
                case 0:
                    {
                        float angle = progress * (float)(Math.PI * 2);
                        float radius = 800f;
                        position.X = startPos.X + (float)(Math.Cos(angle) * radius);
                        position.Z = startPos.Z + (float)(Math.Sin(angle) * radius);
                        position.Y = startPos.Y + (float)(Math.Sin(angle * 2) * 50f);
                        break;
                    }
                case 1:
                    {
                        float angle = progress * (float)(Math.PI * 2);
                        float radius = 600f;
                        position.X = startPos.X + (float)(Math.Sin(angle) * radius);
                        position.Z = startPos.Z + (float)(Math.Sin(angle * 2) * radius * 0.5f);
                        position.Y = startPos.Y + (float)(Math.Cos(angle * 3) * 30f);
                        break;
                    }
                case 2:
                    {
                        float t = (float)(Math.Sin(progress * Math.PI * 2) * 0.5f + 0.5f);
                        Vector3D<float> endPos = startPos + new Vector3D<float>(1200f, 0f, 800f);
                        position = Vector3D.Lerp(startPos, endPos, t);
                        position.Y += (float)(Math.Sin(progress * Math.PI * 4) * 40f);
                        break;
                    }
                case 3:
                    {
                        float angle = progress * (float)(Math.PI * 4);
                        float radius = 400f + progress * 400f;
                        position.X = startPos.X + (float)(Math.Cos(angle) * radius);
                        position.Z = startPos.Z + (float)(Math.Sin(angle) * radius);
                        position.Y = startPos.Y + progress * 200f - 100f;
                        break;
                    }
            }

            return position;
        }

        private static float CalculateBirdRotation(Vector3D<float> currentPos, Vector3D<float> previousPos)
        {
            Vector3D<float> direction = currentPos - previousPos;
            if (direction.LengthSquared > 0.001f)
            {
                return (float)Math.Atan2(direction.X, direction.Z);
            }
            return 0f;
        }
        private static void UpdateBirds(double deltaTime)
        {
            for (int i = 0; i < birds.Count; i++)
            {
                Vector3D<float> previousPosition = birdPositions[i];

                birdFlightProgress[i] += birdFlightSpeeds[i] * (float)deltaTime;

                if (birdFlightProgress[i] > 1.0f)
                    birdFlightProgress[i] -= 1.0f;

                Vector3D<float> newPosition = CalculateBirdPosition(
                    birdStartPositions[i],
                    birdFlightPatterns[i],
                    birdFlightProgress[i]
                );

                float rotation = CalculateBirdRotation(newPosition, previousPosition);
                birdRotations[i] = rotation;

                birdPositions[i] = newPosition;

                float birdScale = 1f;
                var scale = Matrix4X4.CreateScale(birdScale);
                var rotationMatrix = Matrix4X4.CreateRotationY(rotation);
                var translation = Matrix4X4.CreateTranslation(newPosition);
                birdModelMatrices[i] = scale * rotationMatrix * translation;
            }
        }

        private static bool CheckBirdCollisions(Vector3D<float> blimpPosition, float blimpRadius)
        {
            for (int i = 0; i < birdPositions.Count; i++)
            {
                float distance = Vector3D.Distance(blimpPosition, birdPositions[i]);
                float combinedRadius = blimpRadius + birdCollisionRadius;

                if (distance < combinedRadius)
                {
                    Console.WriteLine($"BIRD COLLISION! Bird {i} at distance {distance:F1}");
                    Console.WriteLine($"Bird Position: ({birdPositions[i].X:F1}, {birdPositions[i].Y:F1}, {birdPositions[i].Z:F1})");
                    Console.WriteLine($"Blimp Position: ({blimpPosition.X:F1}, {blimpPosition.Y:F1}, {blimpPosition.Z:F1})");
                    return true;
                }
            }

            return false;
        }

        private static unsafe void DrawBirds()
        {
            for (int i = 0; i < birds.Count; i++)
            {
                SetModelMatrix(birdModelMatrices[i]);
                Gl.BindVertexArray(birds[i].Vao);
                Gl.DrawElements(GLEnum.Triangles, birds[i].IndexArrayLength, GLEnum.UnsignedInt, null);
                Gl.BindVertexArray(0);
            }
        }

        private static unsafe void SetUpObjects()
        {

            float[] face1Color = [1f, 0f, 0f, 1.0f];
            float[] face2Color = [0.0f, 1.0f, 0.0f, 1.0f];
            float[] face3Color = [0.0f, 0.0f, 1.0f, 1.0f];
            float[] face4Color = [1.0f, 0.0f, 1.0f, 1.0f];
            float[] face5Color = [0.0f, 1.0f, 1.0f, 1.0f];
            float[] face6Color = [1.0f, 1.0f, 0.0f, 1.0f];

            blimp = ObjectResourceReader.CreateObjectFromResource(Gl, "blimp.obj");

            float[] tableColor = [System.Drawing.Color.Azure.R/256f,
                                  System.Drawing.Color.Azure.G/256f,
                                  System.Drawing.Color.Azure.B/256f,
                                  1f];
            table = GlCube.CreateSquare(Gl, tableColor);

            CreateMountains();
            CreateCoins();
            CreateBirds();

            skyBox = GlCube.CreateInteriorCube(Gl, "", skyboxSize);
        }

        private static void CreateCoins()
        {
            int coinCount = 30;
            float skyboxRadius = 3500f;
            float minDistanceFromMountains = 250f;
            float minDistanceFromStart = 100f;
            float minDistanceBetweenCoins = 80f;

            coins.Clear();
            coinPositions.Clear();
            coinCollected.Clear();
            coinModelMatrices.Clear();
            coinRotationSpeeds.Clear();
            coinCurrentRotations.Clear();

            Console.WriteLine($"Creating {coinCount} coins");

            for (int i = 0; i < coinCount; i++)
            {
                Vector3D<float> position;
                bool positionValid;
                int attempts = 0;
                const int maxAttempts = 100;

                do
                {
                    float angle = (float)(random.NextDouble() * Math.PI * 2);
                    float distance = (float)(random.NextDouble() * skyboxRadius);

                    position = new Vector3D<float>(
                        (float)(Math.Cos(angle) * distance),
                        40f,
                        (float)(Math.Sin(angle) * distance)
                    );

                    positionValid = true;

                    if (Vector3D.Distance(position, Vector3D<float>.Zero) < minDistanceFromStart)
                    {
                        positionValid = false;
                    }

                    if (positionValid)
                    {
                        foreach (var mountainPos in mountainPositions)
                        {
                            if (Vector3D.Distance(position, mountainPos) < minDistanceFromMountains)
                            {
                                positionValid = false;
                                break;
                            }
                        }
                    }

                    if (positionValid)
                    {
                        foreach (var coinPos in coinPositions)
                        {
                            if (Vector3D.Distance(position, coinPos) < minDistanceBetweenCoins)
                            {
                                positionValid = false;
                                break;
                            }
                        }
                    }

                    attempts++;
                    if (attempts >= maxAttempts)
                    {
                        Console.WriteLine($"Placing coin {i} after {attempts} attempts at suboptimal position");
                        positionValid = true;
                    }
                } while (!positionValid);

                float coinScale = 250f;
                var scale = Matrix4X4.CreateScale(coinScale);

                float initialRotation = (float)(random.NextDouble() * Math.PI * 2);
                float rotationSpeed = (float)(1.0 + random.NextDouble() * 2.0);

                var rotation = Matrix4X4.CreateRotationY(initialRotation);
                var translation = Matrix4X4.CreateTranslation(position);
                var modelMatrix = scale * rotation * translation;

                var coin = ObjectResourceReader.CreateObjectFromResource(Gl, "coin.obj");
                coins.Add(coin);
                coinPositions.Add(position);
                coinCollected.Add(false);
                coinModelMatrices.Add(modelMatrix);
                coinRotationSpeeds.Add(rotationSpeed);
                coinCurrentRotations.Add(initialRotation);

                Console.WriteLine($"Coin {i}: Position({position.X:F1}, {position.Y:F1}, {position.Z:F1})");
            }

            Console.WriteLine($"Successfully created {coins.Count} coins");
        }

        private static void CheckCoinCollection(Vector3D<float> blimpPosition, float blimpRadius)
        {
            for (int i = 0; i < coinPositions.Count; i++)
            {
                if (!coinCollected[i])
                {
                    float distance = Vector3D.Distance(blimpPosition, coinPositions[i]);
                    if (distance < coinCollectionRadius)
                    {
                        coinCollected[i] = true;
                        score++;
                        Console.WriteLine($"Coin {i} collected! Score: {score}");
                    }
                }
            }
        }

        private static bool CheckCollisionWithMountains(Vector3D<float> blimpPosition, float blimpRadius)
        {
            closestMountainDistance = float.MaxValue;
            closestMountainIndex = -1;
            bool collisionOccurred = false;

            for (int i = 0; i < mountainPositions.Count; i++)
            {
                float distance = Vector3D.Distance(blimpPosition, mountainPositions[i]);

                float mountainRadius = mountainCollisionRadii[i];
                float requiredDistance = 1050f;


                if (distance < closestMountainDistance)
                {
                    closestMountainDistance = distance;
                    closestMountainIndex = i;
                }

                if (distance < requiredDistance)
                {
                    Console.WriteLine($"MOUNTAIN COLLISION! Index: {i}");
                    Console.WriteLine($"Blimp Pos: {blimpPosition}");
                    Console.WriteLine($"Mountain Pos: {mountainPositions[i]}");
                    Console.WriteLine($"Distance: {distance} < Required: {requiredDistance}");
                    Console.WriteLine($"Blimp Radius: {blimpRadius}, Mountain Radius: {mountainRadius}");
                    collisionOccurred = true;
                }
            }

            return collisionOccurred;
        }

        private static void ResetGame()
        {
            gameOver = false;
            gameOverMessage = "";
            gameOverTimer = 0f;
            _blimpPosition = Vector3D<float>.Zero;
            score = 0;

            for (int i = 0; i < coinCollected.Count; i++)
            {
                coinCollected[i] = false;
                if (i < coinCurrentRotations.Count)
                {
                    coinCurrentRotations[i] = (float)(random.NextDouble() * Math.PI * 2);
                }
            }

            cameraDescriptor.Reset();
            Console.WriteLine("Game Reset - Score: 0, All coins reset");
        }


        private static void CreateMountains()
        {
            int mountainCount = 20;
            float baseMountainScale = 1000f;
            float scaleVariation = 0.4f;
            float minDistance = 80f;
            float skyboxRadius = 4000f;
            float groundLevel = -1000f;
            float heightVariation = 20f;

            mountains.Clear();
            mountainPositions.Clear();
            mountainScales.Clear();
            mountainModelMatrices.Clear();
            mountainCollisionRadii.Clear();

            Console.WriteLine($"Creating {mountainCount} mountains within radius {skyboxRadius}");

            for (int i = 0; i < mountainCount; i++)
            {
                Vector3D<float> position;
                float scale;
                bool positionValid;
                int attempts = 0;
                const int maxAttempts = 50;

                do
                {
                    if (i < 4)
                    {
                        float angle = i * (float)Math.PI / 2f + (float)(random.NextDouble() - 0.5) * 0.5f;
                        float distance = skyboxRadius * 0.6f + (float)random.NextDouble() * skyboxRadius * 0.3f;

                        position = new Vector3D<float>(
                            (float)(Math.Cos(angle) * distance),
                            groundLevel + (float)(random.NextDouble() * heightVariation - heightVariation / 2),
                            (float)(Math.Sin(angle) * distance)
                        );
                    }
                    else
                    {
                        float angle = (float)(random.NextDouble() * Math.PI * 2);
                        float distance = (float)(random.NextDouble() * skyboxRadius * 0.8f + skyboxRadius * 0.2f);

                        position = new Vector3D<float>(
                            (float)(Math.Cos(angle) * distance),
                            groundLevel + (float)(random.NextDouble() * heightVariation - heightVariation / 2),
                            (float)(Math.Sin(angle) * distance)
                        );
                    }

                    positionValid = true;
                    foreach (var existingPos in mountainPositions)
                    {
                        if (Vector3D.Distance(position, existingPos) < minDistance)
                        {
                            positionValid = false;
                            break;
                        }
                    }

                    if (Vector3D.Distance(position, Vector3D<float>.Zero) < minDistance * 0.5f)
                    {
                        positionValid = false;
                    }

                    attempts++;
                    if (attempts >= maxAttempts)
                    {
                        Console.WriteLine($"Placing mountain {i} after {attempts} attempts at suboptimal position");
                        positionValid = true;
                    }
                } while (!positionValid);

                scale = baseMountainScale * (1f + (float)(random.NextDouble() - 0.5) * scaleVariation);
                var modelMatrix = Matrix4X4.CreateScale(scale) * Matrix4X4.CreateTranslation(position);


                float collisionRadius = scale * 0.8f;

                var mountain = ObjectResourceReader.CreateObjectFromResource(Gl, "mountain2.obj");
                mountains.Add(mountain);
                mountainPositions.Add(position);
                mountainScales.Add(scale);
                mountainModelMatrices.Add(modelMatrix);
                mountainCollisionRadii.Add(collisionRadius);

                Console.WriteLine($"Mountain {i}: Position({position.X:F1}, {position.Y:F1}, {position.Z:F1}), Scale: {scale:F1}, Collision Radius: {collisionRadius:F1}");
            }

            Console.WriteLine($"Successfully created {mountains.Count} mountains");
        }


        private static void Window_Closing()
        {
            blimp.ReleaseGlObject();
            foreach (var mountain in mountains)
            {
                mountain?.ReleaseGlObject();
            }
            foreach (var coin in coins)
            {
                coin?.ReleaseGlObject();
            }
            foreach (var bird in birds)
            {
                bird?.ReleaseGlObject();
            }
        }

        private static unsafe void SetProjectionMatrix()
        {
            var projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>((float)Math.PI / 4f, 1024f / 768f, 0.1f, 10000);
            int location = Gl.GetUniformLocation(program, ProjectionMatrixVariableName);

            if (location == -1)
            {
                throw new Exception($"{ViewMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&projectionMatrix);
            CheckError();
        }

        private static unsafe void SetViewMatrix()
        {
            var viewMatrix = Matrix4X4.CreateLookAt(cameraDescriptor.Position, cameraDescriptor.Target, cameraDescriptor.UpVector);
            int location = Gl.GetUniformLocation(program, ViewMatrixVariableName);

            if (location == -1)
            {
                throw new Exception($"{ViewMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&viewMatrix);
            CheckError();
        }

        public static void CheckError()
        {
            var error = (ErrorCode)Gl.GetError();
            if (error != ErrorCode.NoError)
                throw new Exception("GL.GetError() returned " + error.ToString());
        }
    }
}