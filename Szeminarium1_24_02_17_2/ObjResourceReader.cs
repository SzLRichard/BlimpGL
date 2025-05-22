using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Szeminarium1_24_02_17_2;

namespace Szeminarium1_24_03_05_2
{
    internal class ObjectResourceReader
    {
        public static unsafe GlObject CreateObjectFromResource(GL Gl, string resourceName)
        {
            List<float[]> objVertices = new List<float[]>();
            List<float[]> objNormals = new List<float[]>();
            List<int[]> objFaces = new List<int[]>();
            List<int[]> objNormalIndices = new List<int[]>();

            string fullResourceName = "Szeminarium1_24_02_17_2.Resources." + resourceName;
            using (var objStream = typeof(ObjectResourceReader).Assembly.GetManifestResourceStream(fullResourceName))
            using (var objReader = new StreamReader(objStream))
            {
                while (!objReader.EndOfStream)
                {
                    var line = objReader.ReadLine();

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var firstSpace = line.IndexOf(' ');
                    if (firstSpace == -1)
                        continue;

                    var lineClassifier = line.Substring(0, firstSpace);
                    var lineData = line.Substring(firstSpace + 1).Trim().Split(' ');

                    switch (lineClassifier)
                    {
                        case "v":
                            float[] vertex = new float[3];
                            for (int i = 0; i < vertex.Length; ++i)
                                vertex[i] = float.Parse(lineData[i], CultureInfo.InvariantCulture);
                            objVertices.Add(vertex);
                            break;

                        case "vn":
                            float[] normal = new float[3];
                            for (int i = 0; i < normal.Length; ++i)
                                normal[i] = float.Parse(lineData[i], CultureInfo.InvariantCulture);
                            objNormals.Add(normal);
                            break;

                        case "f":
                            int[] face = new int[3];
                            int[] normalIndex = new int[3];
                            for (int i = 0; i < 3; ++i)
                            {
                                var parts = lineData[i].Split('/');
                                face[i] = int.Parse(parts[0], CultureInfo.InvariantCulture);
                                if (parts.Length >= 3 && !string.IsNullOrEmpty(parts[2]))
                                    normalIndex[i] = int.Parse(parts[2], CultureInfo.InvariantCulture);
                                else
                                    normalIndex[i] = 0;
                            }
                            objFaces.Add(face);
                            objNormalIndices.Add(normalIndex);
                            break;

                        default:
                            break;
                    }
                }
            }

            List<float> glVertices = new List<float>();
            List<float> glColors = new List<float>();
            List<uint> glIndexArray = new List<uint>();

            Dictionary<(int v, int n), uint> uniqueVertexMap = new Dictionary<(int, int), uint>();
            uint nextIndex = 0;

            for (int f = 0; f < objFaces.Count; ++f)
            {
                var face = objFaces[f];
                var normalIndices = objNormalIndices[f];

                for (int i = 0; i < 3; ++i)
                {
                    var key = (face[i], normalIndices[i]);
                    if (!uniqueVertexMap.TryGetValue(key, out uint index))
                    {
                        var vertex = objVertices[face[i] - 1];
                        float nx = 0, ny = 0, nz = 0;
                        if (normalIndices[i] != 0)
                        {
                            var normal = objNormals[normalIndices[i] - 1];
                            nx = normal[0]; ny = normal[1]; nz = normal[2];
                        }

                        glVertices.Add(vertex[0]);
                        glVertices.Add(vertex[1]);
                        glVertices.Add(vertex[2]);

                        glVertices.Add(nx);
                        glVertices.Add(ny);
                        glVertices.Add(nz);

                        glColors.AddRange([1.0f, 0.0f, 0.0f, 1.0f]);

                        index = nextIndex++;
                        uniqueVertexMap[key] = index;
                    }

                    glIndexArray.Add(index);
                }
            }

            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            uint offsetPos = 0;
            uint offsetNormals = offsetPos + 3 * sizeof(float);
            uint vertexSize = offsetNormals + 3 * sizeof(float);

            uint vertices = Gl.GenBuffer();
            Gl.BindBuffer(BufferTargetARB.ArrayBuffer, vertices);
            Gl.BufferData(BufferTargetARB.ArrayBuffer, (ReadOnlySpan<float>)glVertices.ToArray().AsSpan(), BufferUsageARB.StaticDraw);
            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetPos);
            Gl.EnableVertexAttribArray(0);
            Gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, true, vertexSize, (void*)offsetNormals);
            Gl.EnableVertexAttribArray(2);

            uint colors = Gl.GenBuffer();
            Gl.BindBuffer(BufferTargetARB.ArrayBuffer, colors);
            Gl.BufferData(BufferTargetARB.ArrayBuffer, (ReadOnlySpan<float>)glColors.ToArray().AsSpan(), BufferUsageARB.StaticDraw);
            Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(1);

            uint indices = Gl.GenBuffer();
            Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, indices);
            Gl.BufferData(BufferTargetARB.ElementArrayBuffer, (ReadOnlySpan<uint>)glIndexArray.ToArray().AsSpan(), BufferUsageARB.StaticDraw);

            Gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
            Gl.BindVertexArray(0);

            return new GlObject(vao, vertices, colors, indices, (uint)glIndexArray.Count, Gl);
        }
    }
}
