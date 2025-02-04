﻿using System;
using System.Collections.Generic;
using System.Text;
using static GMath.Gfx;
using GMath;
using Rendering;
using static Renderer.Program;
using System.Linq;

namespace Renderer.Modeling
{
    public static class MeshShapeGenerator<T> where T : struct, IVertex<T>, ICoordinatesVertex<T>
    {
        public static Mesh<T> Box(int width, int height, int deep, bool faceXYUp = true, bool faceXYDown = true, bool faceXZUp = true, bool faceXZDown = true, bool faceYZUp = true, bool faceYZDown = true
                                                                 , bool holeXYUp = false, bool holeXYDown = false, bool holeXZUp = false, bool holeXZDown = false, bool holeYZUp = false, bool holeYZDown = false
                                                                 , float4? sepXYUp = null, float4? sepXYDown = null, float4? sepXZUp = null, float4? sepXZDown = null, float4? sepYZUp = null, float4? sepYZDown = null
                                                                 , IMaterial XYUpMat = default, IMaterial XYDownMat = default, IMaterial XZUpMat = default, IMaterial XZDownMat = default, IMaterial YZUpMat = default, IMaterial YZDownMat = default
                                                                 , IMaterial allMat = default)
        {
            if (allMat != default)
            {
                XYDownMat = XYUpMat = XZUpMat = XZDownMat = YZDownMat = YZUpMat = allMat;
            }
            Mesh<T> box = null;
            foreach (var (dirX, dirY, trans) in new (float3 dirX, float3 dirY, float3 trans)[] 
            { 
                (float3(1, 0, 0), float3(0,1,0), float3(0, 0, 0)), 
                (float3(1, 0, 0), float3(0,1,0), float3(0, 0, 1)),
                (float3(1, 0, 0), float3(0,0,1), float3(0, 0, 0)),
                (float3(1, 0, 0), float3(0,0,1), float3(0, 1, 0)),
                (float3(0, 1, 0), float3(0,0,1), float3(0, 0, 0)),
                (float3(0, 1, 0), float3(0,0,1), float3(1, 0, 0)),
            })
            {
                Mesh<T> face = null;
                IMaterial material = default;
                int stacks = 0, slices = 0;
                if (dirX.x != 0)
                {
                    stacks = width;
                    slices = (int)Math.Max(dirY.y * height, dirY.z * deep);
                    if (dirY.y != 0)
                    {
                        material = XYUpMat;
                        if (length(trans) == 0)
                        {
                            if (!faceXYDown)
                                continue;
                            material = XYDownMat;
                            if (holeXYDown)
                            {
                                face = MyManifold<T>.MiddleHoleSurface(slices, stacks, sepXYDown.Value);
                            }
                        }
                        else if (!faceXYUp)
                            continue;
                        else if (holeXYUp)
                        {
                            face = MyManifold<T>.MiddleHoleSurface(slices, stacks, sepXYUp.Value);
                        }
                    }
                    else if (dirY.z != 0)
                    {
                        material = XZUpMat;
                        if (length(trans) == 0)
                        {
                            material = XZDownMat;
                            if (!faceXZDown)
                                continue;
                            if (holeXZDown)
                            {
                                face = MyManifold<T>.MiddleHoleSurface(slices, stacks, sepXZDown.Value).ApplyTransforms(
                            Transforms.RotateX(- pi / 2));
                            }
                        }
                        else if (!faceXZUp)
                            continue;
                        else if (holeXZUp)
                        {
                            face = MyManifold<T>.MiddleHoleSurface(slices, stacks, sepXZUp.Value).ApplyTransforms(
                            Transforms.RotateX(- pi / 2));
                        }
                    }
                }
                else
                {
                    stacks = height;
                    slices = deep;
                    material = YZUpMat;
                    if (length(trans) == 0)
                    {
                        material = YZDownMat;
                        if (!faceYZDown)
                            continue;
                        if (holeYZDown)
                        {
                            face = MyManifold<T>.MiddleHoleSurface(slices, stacks, sepYZDown.Value).ApplyTransforms(
                            Transforms.RotateY(pi / 2));
                        }
                    }
                    else if (!faceYZUp)
                        continue;
                    else if (holeYZUp)
                    {
                        face = MyManifold<T>.MiddleHoleSurface(slices, stacks, sepYZUp.Value).ApplyTransforms(
                            Transforms.RotateY(pi/2));
                    }
                }
                if (face == null)
                    face = Manifold<T>.Surface(stacks, slices, (x, y) => float3(dirX.x * x + trans.x, dirX.y * x + (dirX.y == 0 ? dirY.y * y : 0) + trans.y, dirY.z * y + trans.z));
                else
                {
                    face = face.ApplyTransforms(Transforms.Translate(trans));
                }
                var normal = any(trans) ? trans : -1 + dirX + dirY;
                face.SetNormal(normal);
                face.SetMaterial(material);
                face = MaterialsUtils.MapPlane(face);
                if (box == null)
                    box = face;
                else
                    box += face;
            }
            return box.Transform(Transforms.Translate(-.5f, -.5f, -.5f));
        }


        public static Mesh<T> Box(int points, IMaterial material = default)
        {
            return Box(points / 3, points / 3, points / 3, allMat: material);
        }

        public static Mesh<T> Cylinder(int points, float thickness=0, float angle = 2 * pi, bool surface = false, IMaterial upFaceMat = default, IMaterial downFaceMat = default, IMaterial cylinderMat = default)
        {
            var ss = (int)ceil(sqrt(points));
            Mesh<T> baseCylOuter = null;
            var face1 = MyManifold<T>.Revolution(ss, ss, x => float3(1 * x, 0, 0), float3(0, 0, 1), angle).Transform(Transforms.Translate(0,0,.5f));
            var face2 = MyManifold<T>.Revolution(ss, ss, x => float3(1 * x, 0, 0), float3(0, 0, 1), angle).Transform(Transforms.Translate(0,0,-.5f));
            if (thickness != 0)
            {
                baseCylOuter = MyManifold<T>.Revolution(ss, ss, x => float3(1 + thickness, 0, x), float3(0, 0, 1), angle).Transform(Transforms.Translate(0, 0, -.5f));
                face1 = MyManifold<T>.Revolution(ss, ss, x => float3(1 + x * thickness, 0, 0), float3(0, 0, 1), angle).Transform(Transforms.Translate(0, 0, .5f));
                face2 = MyManifold<T>.Revolution(ss, ss, x => float3(1 + x * thickness, 0, 0), float3(0, 0, 1), angle).Transform(Transforms.Translate(0, 0, -.5f));
            }
            var baseCyl = MyManifold<T>.Revolution(ss, ss, x => float3(1, 0, x), float3(0,0,1), angle).Transform(Transforms.Translate(0,0,-.5f));
            face1.SetMaterial(upFaceMat);
            face1.SetNormal(float3(0, 0, 1));
            face2.SetMaterial(downFaceMat);
            face2.SetNormal(float3(0, 0, -1));
            face1 = MaterialsUtils.MapPlane(face1);
            face2 = MaterialsUtils.MapPlane(face2);
            baseCyl.SetMaterial(cylinderMat);
            if (baseCylOuter == null)
            {
                baseCyl.NormalVertex = new float3[baseCyl.Vertices.Length];
                Array.Copy(baseCyl.Vertices.Select(x => x.Position + 0.1f*x.Position).ToArray(), baseCyl.NormalVertex, baseCyl.Vertices.Length);
                baseCyl.NormalSeparators = new int[baseCyl.Vertices.Length];
                for (int i = 0; i < baseCyl.NormalSeparators.Length; i++)
                {
                    baseCyl.NormalSeparators[i] = i + 1;
                }
            }
            else
            {
                baseCyl.SetNormal(.001f);
                baseCylOuter.NormalVertex = new float3[baseCylOuter.Vertices.Length];
                Array.Copy(baseCylOuter.Vertices.Select(x => x.Position + 0.1f * x.Position).ToArray(), baseCylOuter.NormalVertex, baseCylOuter.Vertices.Length);
                baseCylOuter.NormalSeparators = new int[baseCylOuter.Vertices.Length];
                for (int i = 0; i < baseCylOuter.NormalSeparators.Length; i++)
                {
                    baseCylOuter.NormalSeparators[i] = i + 1;
                }
            }
            baseCylOuter?.SetMaterial(cylinderMat);
            baseCyl = MaterialsUtils.MapCylinderCoordinates(baseCyl);
            if (baseCylOuter != null)
                baseCylOuter = MaterialsUtils.MapCylinderCoordinates(baseCylOuter);
            if (surface)
            {
                return face1;
            }
            if (baseCylOuter != null)
                return baseCyl + face1 + face2 + baseCylOuter;
            else
                return baseCyl + face1 + face2;
        }
    
    }
}
