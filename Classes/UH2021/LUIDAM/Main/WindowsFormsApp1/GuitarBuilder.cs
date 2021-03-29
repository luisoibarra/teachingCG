﻿using Renderer.Modeling;
using Rendering;
using System;
using System.Collections.Generic;
using static GMath.Gfx;
using System.Linq;
using System.Text;
using GMath;

namespace MainForm
{
    public class GuitarBuilder
    {
        public float StringLength => (BridgeLength - BridgeBodyDif)*2;

        public float StringBridgeSeparation => BridgeHeight / 2;

        public float BridgeLength => 28;

        public float BridgeBodyDif => BridgeLength / 4.0f; // How much the bridge is into the body
        
        public float[] StringWidths => new float[] { .06f, .05f, .04f, .04f, .03f, .02f };

        public float BridgeWidth => 4;

        public float BridgeHeight => 1.5f;

        public float BodyWidth => BridgeWidth * 7;

        public Model BridgeStrings()
        {
            var strings = new Model();
            var step = BridgeWidth / (StringWidths.Length + 1);

            for (int i = 0; i < StringWidths.Length; i++)
            {
                var cylinder = ShapeGenerator.Cylinder(1000).ApplyTransforms(Transforms.Translate(0, 0, .5f),
                                                                             Transforms.Scale(StringWidths[i], StringWidths[i], 1),
                                                                             Transforms.Translate(-BridgeWidth / 2 + step * (i + 1), -StringBridgeSeparation, 0));
                strings += cylinder;
            }

            return strings.ApplyTransforms(Transforms.Scale(1, 1, StringLength));
        }

        public Model Bridge()
        {

            var bridge = ShapeGenerator.Box(4000).ApplyTransforms(Transforms.Translate(0, 0, .5f),
                                                                  Transforms.Scale(BridgeWidth, BridgeHeight/2, 1))
                                                 .ApplyFilter(x => x.y != .5f); // Remove face facing the cylinder
            var bridge2 = ShapeGenerator.Cylinder(5000).ApplyFilter(x => x.y > 0) // Remove top half of the cylinder
                                                       .ApplyTransforms(Transforms.Translate(0, 0, .5f),
                                                                        Transforms.Scale(BridgeWidth / 2, BridgeHeight / 2, 1),
                                                                        Transforms.Translate(0, .5f * BridgeHeight / 2, 0));

            var baseBridge = bridge.Min(x => x.z);
            var fretsAmount = 20;
            var step = BridgeLength / fretsAmount;
            var frets = new Model();
            for (int i = 0; i < fretsAmount; i++) // Frets
            {
                var fret = ShapeGenerator.Box(500).ApplyTransforms(Transforms.Translate(0, 0, .5f),
                                                                   Transforms.Scale(BridgeWidth, 1, BridgeWidth / 30),
                                                                   Transforms.Scale(1, i == 0 ? StringBridgeSeparation : BridgeWidth / 30, 1),
                                                                   Transforms.Translate(0, -.5f * BridgeHeight/2, -baseBridge + step * i)); // This is not the correct fret spacing
                frets += fret;
            }

            return (bridge + bridge2).ApplyTransforms(Transforms.Scale(1, 1, BridgeLength)) + frets;
        }

        public Model Headstock()
        {
            var width = BridgeWidth * 1.3f;
            var height = BridgeHeight / 2.0f;
            var length = BridgeWidth * 2;

            var basePiece = ShapeGenerator.Box().ApplyTransforms(Transforms.Translate(0,-.5f,-.5f),
                                                                 Transforms.Scale(width, height, length));
            var xHoleScale = 3 / 16.0f;
            var yHoleScale = 1;
            var zHoleScale = 3 / 4.0f;
            var holeDZ = -length * (1 - zHoleScale) / 2;
            var hole1 = basePiece.ApplyTransforms(Transforms.Scale(xHoleScale, yHoleScale, zHoleScale),
                                                  Transforms.Translate(width*xHoleScale,0, holeDZ));
            var hole2 = hole1.ApplyTransforms(Transforms.Translate(-2 * width * xHoleScale, 0, 0));

            var stringRollCylinders = new Model();
            // ARREGLAR POSICION DE LOS PINES Y QUITAR LOS PUNTOS DE LOS HOLE DEL MODELO DEJANDO LAS CARAS QUE CHOCAN CON LA BASE
            var step = length * zHoleScale / 6.0f;
            for (int i = 0; i < 6; i++)
            {
                var baseCylinder = ShapeGenerator.Cylinder(1000).ApplyTransforms(Transforms.RotateY(pi_over_4 * 2),
                                                                Transforms.Scale(width * xHoleScale, height * yHoleScale * .25f, height * yHoleScale * .25f));

                var zTranslate = ((i%3)+1) * step - length + holeDZ;

                baseCylinder = baseCylinder.ApplyTransforms(Transforms.Translate((i < 3 ? 1 : -1) * 1f * (width * xHoleScale), 0, zTranslate));

                stringRollCylinders += baseCylinder;
            }

            basePiece += hole1 + hole2 + stringRollCylinders;
            return basePiece;
        }

        public Model MainBody()
        {
            // IMPLEMENTATION OF THE GUITAR MAIN BODY

            var bodyLength = BridgeLength * 1.1f;

            var body = ShapeGenerator.Box(10000).ApplyTransforms(Transforms.Translate(0,0,.5f),
                                                                Transforms.Scale(BodyWidth, BridgeHeight*2, bodyLength),
                                                                Transforms.Translate(0, BridgeHeight, BridgeLength - BridgeBodyDif));

            var radius = BridgeWidth * 1.5f / 2;
            var dz = BridgeLength + radius - (BridgeLength / 4 * .2f);
            var c = ShapeGenerator.Cylinder().ApplyTransforms(Transforms.RotateX(pi_over_4 * 2),
                                                              Transforms.Scale(radius, 2, radius),
                                                              Transforms.Translate(0, -.25f, dz));

            var hole = ShapeGenerator.Cylinder(thickness: BridgeWidth/8).ApplyTransforms(Transforms.RotateX(pi_over_4 * 2),
                                                              Transforms.Scale(radius, .01f, radius),
                                                              Transforms.Translate(0, 0, dz));
            
            dz = (c.Max(x => x.z) + c.Min(x => x.z)) / 2;
            var dx = (c.Max(x => x.x) + c.Min(x => x.x)) / 2;
            var minY = c.Min(x => x.y);
            var maxY = c.Max(x => x.y);

            body = body.ApplyFilter(x => !(Math.Pow(x.x - dx, 2) + Math.Pow(x.z - dz, 2) <= Math.Pow(radius, 2) && 
                                         minY <= x.y && x.y <= maxY));

            var stringHub = ShapeGenerator.Box(5000).ApplyTransforms(Transforms.Translate(0, -.5f,.5f),
                                                                     Transforms.Scale(BridgeWidth, 1, 1),
                                                                     Transforms.Translate(0,0,StringLength));
            stringHub += ShapeGenerator.Box(5000).ApplyTransforms(Transforms.Translate(0, -.5f, .5f),
                                                                  Transforms.Scale(BridgeWidth * 1.5f, .5f, 1),
                                                                  Transforms.Translate(0, 0, StringLength));

            return hole + body + stringHub;
        }

        public Model Guitar()
        {
            var body = MainBody() + Bridge();
            return body + BridgeStrings() + Headstock();
            //return MainBody();
        }
    }
}
