﻿using GMath;
using Rendering;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using static GMath.Gfx;

namespace Renderer.Modeling
{
    public static class ShapeGenerator
    {
        /// <summary>
        /// Create a model representing a sphere centered in 0,0,0 with radius 1
        /// </summary>
        /// <param name="pointsAmount"></param>
        /// <returns></returns>
        public static Model Sphere(Color color, int pointsAmount = 10000)
        {
            float3[] points = new float3[pointsAmount];
            var colors = new Color[points.Length];

            for (int i = 0; i < pointsAmount; i++)
            {
                var point = new float3(random(), random(), random());
                point = normalize(point);
                switch ((int)(random() * 8))
                {
                    case 1:
                        point = new float3(point.x, point.y, -point.z);
                        break;
                    case 2:
                        point = new float3(point.x, -point.y, point.z);
                        break;
                    case 3:
                        point = new float3(-point.x, point.y, point.z);
                        break;
                    case 4:
                        point = new float3(point.x, -point.y, -point.z);
                        break;
                    case 5:
                        point = new float3(-point.x, point.y, -point.z);
                        break;
                    case 6:
                        point = new float3(-point.x, -point.y, point.z);
                        break;
                    case 7:
                        point = new float3(-point.x, -point.y, -point.z);
                        break;
                    default:
                        break;
                }
                points[i] = point;
                colors[i] = color;
            }



            return new Model(points, colors);
        }
    
        /// <summary>
        /// Create a model representing a cubic box centered in 0,0,0 with length 1
        /// </summary>
        /// <param name="pointAmounts"></param>
        /// <returns></returns>
        public static Model Box(Color color, int pointAmounts = 10000)
        {
            Func<int, float3, float3, float3[]> GenerateSurface = (amount, vanisher, side) =>
            {
                float3[] points = new float3[amount];
                for (int i = 0; i < amount; i++)
                {
                    points[i] = float3(random(), random(), random()) * vanisher + side;
                }
                return points;
            };

            pointAmounts += pointAmounts % 6;
            float3[] points = new float3[pointAmounts];
            var colors = new Color[points.Length];

            var pointIndex = 0;
            foreach (var (sides, vanisher) in new[] { 
                (float3(1, 0, 0), float3(0, 1, 1)), 
                (float3(0, 0, 0), float3(0, 1, 1)), 
                (float3(0, 1, 0), float3(1, 0, 1)), 
                (float3(0, 0, 0), float3(1, 0, 1)), 
                (float3(0, 0, 1), float3(1, 1, 0)), 
                (float3(0, 0, 0), float3(1, 1, 0)),})
            {
                var amount = pointAmounts / 6;
                var localColors = new Color[amount];
                for (int i = 0; i < localColors.Length; i++)
                {
                    localColors[i] = color;
                }

                Array.Copy(GenerateSurface(amount, vanisher, sides), 0, points, pointIndex, amount);
                Array.Copy(localColors, 0, colors, pointIndex, amount);
                pointIndex += amount;
            }
            return new Model(points, colors).ApplyTransforms(Transforms.Translate(-.5f,-.5f,-.5f));
        }
        
        /// <summary>
        /// Create a cylinder with height in z between -.5 and .5 and radius 1 centered in xy 0,0
        /// </summary>
        /// <param name="pointAmounts"></param>
        /// <returns></returns>
        public static Model Cylinder(Color color, int pointAmounts = 10000, float thickness=0)
        {

            var faceArea = 2 * pi * 1;

            if (thickness != 0)
            {
                faceArea = 2 * pi * (float)Math.Pow(1 + thickness, 2) - faceArea;
            }

            var cylinderArea = 2 * pi * 1 * 1;
            if (thickness != 0)
            {
                cylinderArea += 2 * pi * (1 + thickness) * 1;
            }

            var cylinderAmounts = (int)(cylinderArea / (cylinderArea + faceArea) * pointAmounts);
            
            float3[] points = new float3[cylinderAmounts];
            var colors = new Color[cylinderAmounts];

            for (int i = 0; i < points.Length; i++)
            {
                colors[i] = color;
                float3 point = float3(random(), random(), 0);
                point = normalize(point);
                if (i % 2 == 0)
                    point *= 1 + thickness;

                switch ((int)(random() * 4))
                {
                    case 1:
                        point = new float3(-point.x, point.y, 0);
                        break;
                    case 2:
                        point = new float3(point.x, -point.y, 0);
                        break;
                    case 3:
                        point = new float3(-point.x, -point.y, 0);
                        break;
                    default:
                        break;
                }
                point = float3(point.x, point.y, random() - .5f);
                points[i] = point;
            }

            var faceAmounts = (int)(faceArea / (cylinderArea + faceArea) * pointAmounts);
            float3[] faces = new float3[faceAmounts];
            var facesColors = new Color[faceAmounts];

            for (int i = 0; i < faces.Length; i++)
            {
                facesColors[i] = color;
                float3 point = float3(random(), random(), 0);

                if (thickness == 0)
                    point = normalize(point) * random() + float3(0, 0, i % 2 == 0 ? -.5f : .5f); // Fill cylinder
                else
                    point = normalize(point) * (thickness * random() + 1) + float3(0, 0, i % 2 == 0 ? -.5f : .5f); // Fill like a pipe

                switch ((int)(random() * 4))
                {
                    case 1:
                        point *= float3(-1, 1, 1);
                        break;
                    case 2:
                        point *= float3(1, -1, 1);
                        break;
                    case 3:
                        point *= float3(-1, -1, 1);
                        break;
                    default:
                        break;
                }

                faces[i] = point;
            }

            return new Model(points, colors) + new Model(faces, facesColors);
        }
    }
}
