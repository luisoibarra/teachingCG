﻿using GMath;
using Rendering;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static GMath.Gfx;

namespace Rendering
{
    public class Mesh<V> : IEnumerable<V> where V : struct, IVertex<V>
    {
        /// <summary>
        /// Gets the extreme points of the bounding box of that contains all mesh points
        /// </summary>
        public (float3 topCorner, float3 oppositeCorner) BoundBox;

        /// <summary>
        /// Gets the vertices of this mesh.
        /// </summary>
        public V[] Vertices { get; private set; }

        /// <summary>
        /// Gets the indices of this mesh. Depending on the topology, this array is grouped by 1, 2 or 3 to form the mesh points, edges or faces.
        /// </summary>
        public int[] Indices { get; private set; }

        /// <summary>
        /// Gets the topology of this mesh. Points will use an index per point. Lines will use two indices per line. Triangles will use three indices per triangle.
        /// </summary>
        public Topology Topology { get; private set; }

        /// <summary>
        /// Creates a mesh object using vertices, indices and the desired topology.
        /// </summary>
        public Mesh(V[] vertices, int[] indices, Topology topology = Topology.Triangles)
        {
            this.Vertices = vertices;
            this.Indices = indices;
            this.Topology = topology;

            if (Vertices.Any())
                BoundBox = (float3(Vertices.Max(x => x.Position.x), Vertices.Max(x => x.Position.y), Vertices.Max(x => x.Position.z)),
                            float3(Vertices.Min(x => x.Position.x), Vertices.Min(x => x.Position.y), Vertices.Min(x => x.Position.z)));
        }

        public Mesh() : this(new V[] { }, new int[] { })
        {

        }

        /// <summary>
        /// Gets a new mesh instance with vertices and indices clone.
        /// </summary>
        /// <returns></returns>
        public Mesh<V> Clone()
        {
            V[] newVertices = Vertices.Clone() as V[];
            int[] newIndices = Indices.Clone() as int[];
            return new Mesh<V>(newVertices, newIndices, this.Topology);
        }

        #region Mesh Vertices Transforms

        public Mesh<T> Transform<T>(Func<V, T> transform) where T : struct, IVertex<T>
        {
            T[] newVertices = new T[Vertices.Length];

            for (int i = 0; i < newVertices.Length; i++)
                newVertices[i] = transform(Vertices[i]);

            return new Mesh<T>(newVertices, Indices, Topology);
        }

        public Mesh<V> Transform(Func<V, V> transform)
        {
            return Transform<V>(transform);
        }

        public Mesh<V> Transform(float4x4 transform)
        {
            return Transform(v =>
            {
                float4 hP = float4(v.Position, 1);
                hP = mul(hP, transform);
                V newVertex = v;
                newVertex.Position = hP.xyz / hP.w;
                return newVertex;
            });
        }

        public Mesh<V> ApplyTransforms(params float4x4[] transforms)
        {
            var id = Transforms.Identity;
            foreach (var item in transforms)
            {
                id = mul(id, item);
            }
            return Transform(id);
        }

        #endregion

        /// <summary>
        /// Changes a mesh to another object with different topology. For instance, from a triangle mesh to a wireframe (lines).
        /// </summary>
        public Mesh<V> ConvertTo(Topology topology)
        {
            switch (topology)
            {
                case Topology.Triangles:
                    switch (this.Topology)
                    {
                        case Topology.Triangles:
                            return this.Clone(); // No necessary change
                        case Topology.Lines:
                            // This problem is NP.
                            // Try to implement a greedy, that means, recognize the small triangle and so on...
                            throw new NotImplementedException("Missing implementing line-to-triangle conversion.");
                        case Topology.Points:
                            throw new NotImplementedException("Missing implementing point-to-triangle conversion.");
                    }
                    break;
                case Topology.Lines:
                    switch (this.Topology)
                    {
                        case Topology.Points:
                            // Get the wireframe from surface reconstruction
                            return ConvertTo(Topology.Triangles).ConvertTo(Topology.Lines);
                        case Topology.Lines:
                            return this.Clone(); // nothing to do
                        case Topology.Triangles:
                            {
                                // This is repeating edges for adjacent triangles.... use a hash table to prevent for double linking vertices.
                                V[] newVertices = Vertices.Clone() as V[];
                                int[] newIndices = new int[Indices.Length * 2];
                                int index = 0;
                                for (int i = 0; i < Indices.Length / 3; i++)
                                {
                                    newIndices[index++] = Indices[i * 3 + 0];
                                    newIndices[index++] = Indices[i * 3 + 1];

                                    newIndices[index++] = Indices[i * 3 + 1];
                                    newIndices[index++] = Indices[i * 3 + 2];

                                    newIndices[index++] = Indices[i * 3 + 2];
                                    newIndices[index++] = Indices[i * 3 + 0];
                                }
                                return new Mesh<V>(newVertices, newIndices, Topology.Lines);
                            }
                    }
                    break;
                case Topology.Points:
                    {
                        V[] newVertices = Vertices.Clone() as V[];
                        int[] indices = new int[newVertices.Length];
                        for (int i = 0; i < indices.Length; i++)
                            indices[i] = i;
                        return new Mesh<V>(newVertices, indices, Topology.Points);
                    }
            }

            throw new ArgumentException("Wrong topology.");
        }

        public IEnumerator<V> GetEnumerator()
        {
            return ((IEnumerable<V>)Vertices).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Vertices.GetEnumerator();
        }

        public static Mesh<V> operator +(Mesh<V> a, Mesh<V> b)
        {
            return new Mesh<V>(a.Vertices.Concat(b.Vertices).ToArray(),
                               a.Indices.Concat(b.Indices.Select(x => x + a.Vertices.Length)).ToArray());
        }

        /// <summary>
        /// Returs a model with the points between (0,0,0) <= (x,y,z) <= (wwidth, height, deep)
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="deep"></param>
        /// <returns></returns>
        public Mesh<V> FitIn(float width, float height, float deep)
        {
            //var model = this.ApplyTransforms(Transforms.Translate(-BoundBox.oppositeCorner));
            //var scale = new float[] { width / (model.BoundBox.topCorner.x), height / (model.BoundBox.topCorner.y), deep / (model.BoundBox.topCorner.z) }.Min();
            //model = model.ApplyTransforms(Transforms.Scale(scale, scale, scale));
            //return model;
            var s = Transforms.FitIn(BoundBox.oppositeCorner, BoundBox.topCorner, width, height, deep);
            return this.Transform(s);
        }

    }


    public static class MeshTools
    {
        #region Mesh Vertices Transforms

        public static Mesh<T> Transform<V, T>(this Mesh<V> mesh, Func<V, T> transform) where V : struct, IVertex<V> where T : struct, IVertex<T>
        {
            T[] newVertices = new T[mesh.Vertices.Length];

            for (int i = 0; i < newVertices.Length; i++)
                newVertices[i] = transform(mesh.Vertices[i]);

            return new Mesh<T>(newVertices, mesh.Indices, mesh.Topology);
        }

        public static Mesh<V> Transform<V>(this Mesh<V> mesh, Func<V, V> transform) where V : struct, IVertex<V>
        {
            return Transform<V, V>(mesh, transform);
        }

        public static Mesh<V> Transform<V>(this Mesh<V> mesh, float4x4 transform) where V : struct, IVertex<V>
        {
            return Transform<V>(mesh, v =>
            {
                float4 hP = float4(v.Position, 1);
                hP = mul(hP, transform);
                V newVertex = v;
                newVertex.Position = hP.xyz / hP.w;
                return newVertex;
            });
        }

        #endregion

        /// <summary>
        /// Changes a mesh to another object with different topology. For instance, from a triangle mesh to a wireframe (lines).
        /// </summary>
        public static Mesh<V> ConvertTo<V>(this Mesh<V> mesh, Topology topology) where V : struct, IVertex<V>
        {
            switch (topology)
            {
                case Topology.Triangles:
                    switch (mesh.Topology)
                    {
                        case Topology.Triangles:
                            return mesh.Clone(); // No necessary change
                        case Topology.Lines:
                            // This problem is NP.
                            // Try to implement a greedy, that means, recognize the small triangle and so on...
                            throw new NotImplementedException("Missing implementing line-to-triangle conversion.");
                        case Topology.Points:
                            throw new NotImplementedException("Missing implementing point-to-triangle conversion.");
                    }
                    break;
                case Topology.Lines:
                    switch (mesh.Topology)
                    {
                        case Topology.Points:
                            // Get the wireframe from surface reconstruction
                            return mesh.ConvertTo(Topology.Triangles).ConvertTo(Topology.Lines);
                        case Topology.Lines:
                            return mesh.Clone(); // nothing to do
                        case Topology.Triangles:
                            {
                                // This is repeating edges for adjacent triangles.... use a hash table to prevent for double linking vertices.
                                V[] newVertices = mesh.Vertices.Clone() as V[];
                                int[] newIndices = new int[mesh.Indices.Length * 2];
                                int index = 0;
                                for (int i = 0; i < mesh.Indices.Length / 3; i++)
                                {
                                    newIndices[index++] = mesh.Indices[i * 3 + 0];
                                    newIndices[index++] = mesh.Indices[i * 3 + 1];

                                    newIndices[index++] = mesh.Indices[i * 3 + 1];
                                    newIndices[index++] = mesh.Indices[i * 3 + 2];

                                    newIndices[index++] = mesh.Indices[i * 3 + 2];
                                    newIndices[index++] = mesh.Indices[i * 3 + 0];
                                }
                                return new Mesh<V>(newVertices, newIndices, Topology.Lines);
                            }
                    }
                    break;
                case Topology.Points:
                    {
                        V[] newVertices = mesh.Vertices.Clone() as V[];
                        int[] indices = new int[newVertices.Length];
                        for (int i = 0; i < indices.Length; i++)
                            indices[i] = i;
                        return new Mesh<V>(newVertices, indices, Topology.Points);
                    }
            }

            throw new ArgumentException("Wrong topology.");
        }

        /// <summary>
        /// Welds different vertices with positions close to each other using an epsilon decimation.
        /// </summary>
        public static Mesh<V> Weld<V>(this Mesh<V> mesh, float epsilon = 0.0001f) where V : struct, IVertex<V>
        {
            // Method using decimation...
            // TODO: Implement other methods

            Dictionary<int3, int> uniqueVertices = new Dictionary<int3, int>();
            int[] mappedVertices = new int[mesh.Vertices.Length];
            List<V> newVertices = new List<V>();

            for (int i = 0; i < mesh.Vertices.Length; i++)
            {
                V vertex = mesh.Vertices[i];
                float3 p = vertex.Position;
                int3 cell = (int3)(p / epsilon); // convert vertex position in a discrete cell.
                if (!uniqueVertices.ContainsKey(cell))
                {
                    uniqueVertices.Add(cell, newVertices.Count);
                    newVertices.Add(vertex);
                }
                mappedVertices[i] = uniqueVertices[cell];
            }

            int[] newIndices = new int[mesh.Indices.Length];
            for (int i = 0; i < mesh.Indices.Length; i++)
                newIndices[i] = mappedVertices[mesh.Indices[i]];

            return new Mesh<V>(newVertices.ToArray(), newIndices, mesh.Topology);
        }

        public static void ComputeNormals<V>(this Mesh<V> mesh) where V : struct, INormalVertex<V>
        {
            if (mesh.Topology != Topology.Triangles)
                return;

            float3[] normals = new float3[mesh.Vertices.Length];

            for (int i = 0; i < mesh.Indices.Length / 3; i++)
            {
                float3 p0 = mesh.Vertices[mesh.Indices[i * 3 + 0]].Position;
                float3 p1 = mesh.Vertices[mesh.Indices[i * 3 + 1]].Position;
                float3 p2 = mesh.Vertices[mesh.Indices[i * 3 + 2]].Position;

                // Compute the normal of the triangle.
                float3 N = cross(p1 - p0, p2 - p0);

                // Add the normal to the vertices involved
                normals[mesh.Indices[i * 3 + 0]] += N;
                normals[mesh.Indices[i * 3 + 1]] += N;
                normals[mesh.Indices[i * 3 + 2]] += N;
            }

            // Update per-vertex normal using normal accumulation normalized.
            for (int i = 0; i < mesh.Vertices.Length; i++)
                mesh.Vertices[i].Normal = normalize(normals[i]);
        }

    }

    /// <summary>
    /// Tool class to create different mesh from parametric methods.
    /// </summary>
    public class Manifold<V> where V : struct, IVertex<V>
    {
        public static Mesh<V> Surface(int slices, int stacks, Func<float, float, float3> generating)
        {
            V[] vertices = new V[(slices + 1) * (stacks + 1)];
            int[] indices = new int[slices * stacks * 6];

            // Filling vertices for the manifold.
            // A manifold with x,y,z mapped from (0,0)-(1,1)
            for (int i = 0; i <= stacks; i++)
                for (int j = 0; j <= slices; j++)
                    vertices[i * (slices + 1) + j] = new V { Position = generating(j / (float)slices, i / (float)stacks) };

            // Filling the indices of the quad. Vertices are linked to adjacent.
            int index = 0;
            for (int i = 0; i < stacks; i++)
                for (int j = 0; j < slices; j++)
                {
                    indices[index++] = i * (slices + 1) + j;
                    indices[index++] = (i + 1) * (slices + 1) + j;
                    indices[index++] = (i + 1) * (slices + 1) + (j + 1);

                    indices[index++] = i * (slices + 1) + j;
                    indices[index++] = (i + 1) * (slices + 1) + (j + 1);
                    indices[index++] = i * (slices + 1) + (j + 1);
                }

            return new Mesh<V>(vertices, indices);
        }

        public static Mesh<V> Generative(int slices, int stacks, Func<float, float3> g, Func<float3, float, float3> f)
        {
            return Surface(slices, stacks, (u, v) => f(g(u), v));
        }

        public static Mesh<V> Extrude(int slices, int stacks, Func<float, float3> g, float3 direction)
        {
            return Generative(slices, stacks, g, (v, t) => v + direction * t);
        }

        public static Mesh<V> Revolution(int slices, int stacks, Func<float, float3> g, float3 axis, float angle = 2 * pi)
        {
            return Generative(slices, stacks, g, (v, t) => mul(float4(v, 1), Transforms.Rotate(t * angle, axis)).xyz);
        }

        public static Mesh<V> Lofted(int slices, int stacks, Func<float, float3> g1, Func<float, float3> g2)
        {
            return Surface(slices, stacks, (u, v) => g1(u) * (1 - v) + g2(u) * v);
        }

        /// <summary>
        /// Builds a surface in xy plane with a hole
        /// Boundries are (0,0,0) and (1,1,0)
        /// </summary>
        /// <param name="slices"></param>
        /// <param name="stacks"></param>
        /// <param name="separation">starting left in clockwise, separation of the hole from the surface borders</param>
        /// <returns></returns>
        public static Mesh<V> MiddleHoleSurface(int slices, int stacks, float4 separation)
        {
            static float3 meshGenerator(float x, float y, float z, float3 center, float radius, float3 centerDir)
            {
                var p = float3(x, y, z);
                var d = p - center;
                var lengthD = length(d);
                if (lengthD <= .00001f)
                {
                    d = p + .001f * normalize(centerDir) - center;
                }
                if (lengthD <= radius) // Inside sphere
                {
                    // a,b,c Components of the parametric substitution of ray(center,d) in the sphere equation
                    var sphereA = pow(d.x, 2) + pow(d.y, 2) + pow(d.z, 2);
                    var alpha = radius * sqrt(1 / sphereA);
                    var intersection = center + alpha * d;
                    p = intersection;
                }
                return p;
            }

            var radius = 1f;
            var center = float3(1, 0, 0);
            var radiusFixScale = .8f; // To fix near center points issue 
            var holeXScale = 1 - separation.x - separation.z;
            var holeYScale = 1 - separation.y - separation.w;


            var model = Surface(slices / 2, stacks / 2,
                (x, y) => meshGenerator(2 * x, - 1 + y * radiusFixScale, 0, center, radius, float3(0, -1, 0)))
                .Transform(Transforms.Translate((1-radiusFixScale)*float3(0,1,0)));

            model += Surface(slices / 2, stacks / 2,
                (x, y) => meshGenerator(2 * x, 1 - y * radiusFixScale, 0, center, radius, float3(0, 1, 0)))
                .Transform(Transforms.Translate((1-radiusFixScale)*float3(0,-1,0)));

            model = model.FitIn(1, 1, 1);
            model = model.ApplyTransforms(Transforms.Scale(1 / (model.BoundBox.topCorner.x == 0 ? 1 : model.BoundBox.topCorner.x),
                                                           1 / (model.BoundBox.topCorner.y == 0 ? 1 : model.BoundBox.topCorner.y), 
                                                           1 / (model.BoundBox.topCorner.z == 0 ? 1 : model.BoundBox.topCorner.z)));
            model = model.FitIn(1 - separation.x - separation.z, 1 - separation.y - separation.w, 1);
            model = model.ApplyTransforms(Transforms.Translate(separation.x, separation.w, 0));
            var l = model.BoundBox.oppositeCorner;
            var h = model.BoundBox.topCorner;

            var upSurface        = Surface((int)ceil(slices * holeXScale), (int)ceil(stacks * separation.y), 
                (x, y) => float3(l.x + x * (1 - separation.x - separation.z), h.y + y * separation.y, 0));
            
            var downSurface      = Surface((int)ceil(slices * holeXScale), (int)ceil(stacks * separation.w), 
                (x, y) => float3(l.x + x * (1 - separation.x - separation.z), y * separation.w, 0));
            
            var leftSurface      = Surface((int)ceil(slices * separation.x), (int)ceil(stacks * holeYScale), 
                (x, y) => float3(x * separation.x, l.y + y * (1 - separation.w - separation.y), 0));
            
            var rightSurface     = Surface((int)ceil(slices * separation.z), (int)ceil(stacks * holeYScale), 
                (x, y) => float3(h.x + x * separation.z, l.y + y * (1 - separation.w - separation.y), 0));
            
            var upLeftSurface    = Surface((int)ceil(slices * separation.x), (int)ceil(stacks * separation.y),
                (x, y) => float3(x * separation.x, h.y + y * separation.y, 0));
            
            var upRightSurface   = Surface((int)ceil(slices * separation.z), (int)ceil(stacks * separation.y),
                (x, y) => float3(h.x + x * separation.z, h.y + y * separation.y, 0));
            
            var downLeftSurface  = Surface((int)ceil(slices * separation.x), (int)ceil(stacks * separation.w),
                (x, y) => float3(x * separation.x, y * separation.w, 0));
            
            var downRightSurface = Surface((int)ceil(slices * separation.z), (int)ceil(stacks * separation.w),
                (x, y) => float3(h.x + x * separation.z, y * separation.w, 0));
            
            var surface = upSurface + downSurface + leftSurface + rightSurface + upLeftSurface + upRightSurface + downLeftSurface + downRightSurface;
            return model + surface;
        }
    }

    /// <summary>
    /// Represents different topologies to connect vertices.
    /// </summary>
    public enum Topology
    {
        /// <summary>
        /// Every vertex is a different point.
        /// </summary>
        Points,
        /// <summary>
        /// Every two vertices there is a line in between.
        /// </summary>
        Lines,
        /// <summary>
        /// Every three vertices form a triangle
        /// </summary>
        Triangles
    }
}
