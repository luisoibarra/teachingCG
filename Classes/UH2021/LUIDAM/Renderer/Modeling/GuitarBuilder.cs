﻿using Renderer.Modeling;
using Rendering;
using System;
using System.Collections.Generic;
using static GMath.Gfx;
using System.Linq;
using System.Text;
using GMath;
using System.Drawing;
using static Renderer.Program;
using Renderer;
using System.IO;
using Renderer.CSG;

namespace Renderer
{
    public class GuitarBuilder<T> where T : struct, IVertex<T>, INormalVertex<T>, ICoordinatesVertex<T>, IColorable, ITransformable<T>
    {
        #region Scalars

        public float MeshScalar = 1f;

        public float StringLength => (BridgeLength - BridgeBodyDif)*2;

        public float StringBridgeSeparation => BridgeHeight / 2;

        public float BridgeLength => 28;

        public float BridgeBodyDif => BridgeLength / 4.0f; // How much the bridge is into the body

        public float BridgeWidth => 4;

        public float BridgeHeight => 1.5f;

        public float BodyWidth => BridgeWidth * 7;

        #endregion
       
        #region Materials

        public MyMaterial<T>[] StringMaterials => new MyMaterial<T>[] 
        { MetalStringMaterial, MetalStringMaterial, MetalStringMaterial, NylonStringMaterial, NylonStringMaterial, NylonStringMaterial}; // TODO
        public static MyMaterial<T> StringCylinderMaterial  = Materials<T>.BasePinHeadMaterial; // TODO
        public static MyMaterial<T> HeadPinMaterial         = Materials<T>.BasePinHeadMaterial; // TODO
        public static MyMaterial<T> FretMaterial            = Materials<T>.BasePinMaterial; // TODO
        public static MyMaterial<T> BasePinMaterial         = Materials<T>.BasePinMaterial; // TODO
        public static MyMaterial<T> MetalStringMaterial     = Materials<T>.MetalStringMaterial; // TODO
        public static MyMaterial<T> NylonStringMaterial     = Materials<T>.NylonStringMaterial; // TODO
        public static MyMaterial<T> GuitarHoleMaterial      = Materials<T>.GuitarBodyHoleMaterial; // TODO
        public static MyMaterial<T> BridgeMaterial          = Materials<T>.BackMainGuitarBodyMaterial; // TODO
        public static MyMaterial<T> StringHubMaterial       = Materials<T>.BackMainGuitarBodyMaterial; // TODO
        public static MyMaterial<T> HeadstockMaterial       = Materials<T>.BackMainGuitarBodyMaterial; // TODO
        public static MyMaterial<T> GuitarBodySidesMaterial = Materials<T>.BackMainGuitarBodyMaterial; // TODO
        public static MyMaterial<T> GuitarBodyFrontMaterial = Materials<T>.FrontMainGuitarBodyMaterial; // TODO
        public static MyMaterial<T> GuitarBodyBackMaterial  = Materials<T>.BackMainGuitarBodyMaterial; // TODO
        public static MyMaterial<T> StringHubTableMaterial  = Materials<T>.StringHubTablesMaterial;

        #endregion

        #region Colors

        public Color BridgeUpperColor => Color.FromArgb(65, 54, 52);

        public Color BridgeLowerColor => Color.FromArgb(65, 54, 52);

        public Color BodyColor => Color.FromArgb(232, 12, 128);

        public Color StringHubColor => Color.FromArgb(42, 31, 27);

        public Color PinColor => Color.LightYellow;

        public Color HeadPinColor => Color.WhiteSmoke;

        public Color FretColor => Color.SlateGray;

        public Color HoleColor => Color.FromArgb(85, 43, 21);

        public Color HeadstockColor => Color.FromArgb(85,43,21);

        public Color BaseCylinderColor => Color.FromArgb(255, 255, 255);

        public float[] StringWidths => new float[] { .06f, .05f, .04f, .04f, .03f, .02f };

        public Color[] StringColors => new Color[] { Color.DarkSlateGray, Color.DarkSlateGray, Color.DarkSlateGray, Color.DarkSlateGray, Color.DarkSlateGray, Color.DarkSlateGray };

        #endregion
        
      
        #region Dots

        public Model BridgeStrings()
        {
            var strings = new Model();
            var step = BridgeWidth / (StringWidths.Length + 1);

            for (int i = 0; i < StringWidths.Length; i++)
            {
                var cylinder = ShapeGenerator.Cylinder(StringColors[i], 6000).ApplyTransforms(Transforms.Translate(0, 0, .5f),
                                                                             Transforms.Scale(StringWidths[i], StringWidths[i], 1),
                                                                             Transforms.Translate(-BridgeWidth / 2 + step * (i + 1), -StringBridgeSeparation, 0));
                strings += cylinder;
            }

            return strings.ApplyTransforms(Transforms.Scale(1, 1, StringLength));
        }

        public Model Bridge()
        {

            var bridge = ShapeGenerator.Box(BridgeUpperColor, 10000).ApplyTransforms(Transforms.Translate(0, 0, .5f),
                                                                  Transforms.Scale(BridgeWidth, BridgeHeight/2, 1))
                                                 .ApplyFilter(x => x.y != .5f); // Remove face facing the cylinder
            var bridge2 = ShapeGenerator.Cylinder(BridgeLowerColor, 10000).ApplyFilter(x => x.y > 0) // Remove top half of the cylinder
                                                       .ApplyTransforms(Transforms.Translate(0, 0, .5f),
                                                                        Transforms.Scale(BridgeWidth / 2, BridgeHeight / 2, 1),
                                                                        Transforms.Translate(0, .5f * BridgeHeight / 2, 0));

            var baseBridge = bridge.Min(x => x.Item1.z);
            var fretsAmount = 20;
            var step = BridgeLength / fretsAmount;
            var frets = new Model();
            for (int i = 0; i < fretsAmount; i++) // Frets
            {
                var fret = ShapeGenerator.Box(FretColor, 500).ApplyTransforms(Transforms.Translate(0, 0, .5f),
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

            var basePiece = ShapeGenerator.Box(HeadstockColor).ApplyTransforms(Transforms.Translate(0,-.5f,-.5f),
                                                                 Transforms.Scale(width, height, length));
            var xHoleScale = 3 / 16.0f;
            var yHoleScale = 1;
            var zHoleScale = 3 / 4.0f;
            var holeDZ = -length * (1 - zHoleScale) / 2;
            var hole1 = basePiece.ApplyTransforms(Transforms.Scale(xHoleScale, yHoleScale*2f, zHoleScale),
                                                  Transforms.Translate(width*xHoleScale,height/2, holeDZ));
            var hole2 = hole1.ApplyTransforms(Transforms.Translate(-2 * width * xHoleScale, 0, 0));

            basePiece -= hole1;
            basePiece -= hole2;

            var stringRollCylinders = new Model();
            var stringPins = new Model();
            var step = length * zHoleScale / 3.0f;
            for (int i = 0; i < 6; i++)
            {
                var xBaseCylinderScale = width * xHoleScale;

                var baseCylinder = ShapeGenerator.Cylinder(PinColor, 1000).ApplyTransforms(Transforms.RotateY(pi_over_4 * 2),
                                                                                 Transforms.Scale(xBaseCylinderScale, height * yHoleScale * .25f, height * yHoleScale * .25f));

                var zTranslate = ((i%3)) * step - length - holeDZ + step/2;
                var yTranslate = -height / 2.0f;

                baseCylinder = baseCylinder.ApplyTransforms(Transforms.Translate((i < 3 ? 1 : -1) * 1f * (width * xHoleScale), yTranslate, zTranslate));

                stringRollCylinders += baseCylinder;

                var xPinScale = xBaseCylinderScale / 3;
                var yPinScale = height * .2f;

                var basePin = ShapeGenerator.Cylinder(PinColor, 1000).ApplyTransforms(Transforms.RotateY(pi_over_4 * 2),
                                                                            Transforms.Scale(xPinScale, yPinScale, yPinScale));

                var yHolderScale = height * 1.5f;
                var xHolderScale = xPinScale / 4;
                var headHolder = ShapeGenerator.Cylinder(PinColor, 1000).ApplyTransforms(Transforms.RotateX(pi_over_4 * 2),
                                                                               Transforms.Translate(0, .5f, 0),
                                                                               Transforms.Scale(xHolderScale, yHolderScale, xHolderScale),
                                                                               Transforms.Translate((i < 3 ? 1 : -1) * xHolderScale, -yPinScale, -xHolderScale*2));

                var head = ShapeGenerator.Cylinder(HeadPinColor, 1000).ApplyTransforms(Transforms.RotateX(pi_over_4 * 2),
                                                                         Transforms.Scale(1.1f*xHolderScale, 2*yHolderScale/6, 2*yHolderScale/3),
                                                                         Transforms.RotateY((two_pi/12*i)),
                                                                         Transforms.Translate((i < 3 ? 1 : -1) * xHolderScale, -yPinScale + yHolderScale, -xHolderScale * 2));


                var pin = basePin + headHolder + head;

                pin = pin.ApplyTransforms(Transforms.Translate((i < 3 ? 1 : -1) * (width/2 + xPinScale/2), yTranslate, zTranslate));
                
                stringPins += pin;
            }

            basePiece += stringRollCylinders + stringPins;
            return basePiece;
        }

        public Model MainBody()
        {
            // IMPLEMENTATION OF THE GUITAR MAIN BODY

            var bodyLength = BridgeLength * 1.1f;

            float bound = 2.697f;
            //float transform(float x, float val) => (x)*(-((val* bound) -1)*((val * bound) - 1)*((val * bound) - 2.3f)*((val * bound) - 3)+1); // -0.5 <= x <= 0.5   &&   0 <= z <= 1

            Func<float, float2> bazier = BodyFunction();
            float transform(float x, float val) => x * (bazier(val*2.9f)).y;

            var body = ShapeGenerator.Box(BodyColor, 40000).ApplyTransforms(Transforms.Translate(0,0,.5f))
                                                .ApplyFreeTransform(p => float3( transform(p.x, p.z), p.y, p.z))
                                                .ApplyTransforms(Transforms.Translate(0,0,-.5f));
            
            body = body.ApplyTransforms(Transforms.Translate(0,0,.5f),
                                        Transforms.Scale(BodyWidth, BridgeHeight*2, bodyLength),
                                        Transforms.Translate(0, BridgeHeight, BridgeLength - BridgeBodyDif));

            var radius = BridgeWidth * 1.5f / 2;
            var dz = BridgeLength + radius - (BridgeLength / 4 * .2f);
            var c = ShapeGenerator.Cylinder(Color.Red).ApplyTransforms(Transforms.RotateX(pi_over_4 * 2),
                                                              Transforms.Scale(radius, 2, radius),
                                                              Transforms.Translate(0, -.25f, dz));

            var hole = ShapeGenerator.Cylinder(HoleColor, thickness: BridgeWidth/8).ApplyTransforms(Transforms.RotateX(pi_over_4 * 2),
                                                              Transforms.Scale(radius, .01f, radius),
                                                              Transforms.Translate(0, 0, dz));
            
            dz = (c.Max(x => x.Item1.z) + c.Min(x => x.Item1.z)) / 2;
            var dx = (c.Max(x => x.Item1.x) + c.Min(x => x.Item1.x)) / 2;
            var minY = c.Min(x => x.Item1.y);
            var maxY = c.Max(x => x.Item1.y);

            body = body.ApplyFilter(x => !(Math.Pow(x.x - dx, 2) + Math.Pow(x.z - dz, 2) <= Math.Pow(radius, 2) && 
                                         minY <= x.y && x.y <= maxY));

            var stringHub = ShapeGenerator.Box(StringHubColor, 3000).ApplyTransforms(Transforms.Translate(0, -.5f,.5f),
                                                                     Transforms.Scale(BridgeWidth, 1, 1.5f),
                                                                     Transforms.Translate(0,0,StringLength));
            stringHub += ShapeGenerator.Box(StringHubColor, 6000).ApplyTransforms(Transforms.Translate(0, -.5f, .5f),
                                                                  Transforms.Scale(BridgeWidth * 3f, .5f, 1.5f),
                                                                  Transforms.Translate(0, 0, StringLength));

            return hole + body + stringHub;
        }

        public Model Guitar()
        {
            var body = MainBody() + Bridge();
            return body + BridgeStrings() + Headstock();
            //return MainBody();
        }

        #endregion

        #region Mesh

        public Mesh<T> BridgeStringMesh() 
        {
            Mesh<T> strings = null;
            var step = BridgeWidth / (StringWidths.Length + 1);

            for (int i = 0; i < StringWidths.Length; i++)
            {
                var material = StringMaterials[i];
                var cylinder = MeshShapeGenerator<T>.Cylinder((int)(40 * MeshScalar),upFaceMat:material, downFaceMat:material, cylinderMat:material)
                                                                          .ApplyTransforms(Transforms.Translate(0, 0, .5f),
                                                                                           Transforms.Scale(StringWidths[i], StringWidths[i], 1),
                                                                                           Transforms.Translate(-BridgeWidth / 2 + step * (i + 1), -StringBridgeSeparation, 0));
                AddColorToMesh(StringColors[i], cylinder);
                if (strings == null)
                    strings = cylinder;
                else
                    strings += cylinder;
            }
            return strings.ApplyTransforms(Transforms.Scale(1, 1, StringLength + 1.2f));
        }
    
        public Mesh<T> BridgeMesh()
        {
            var bridge = MeshShapeGenerator<T>.Box((int)(MeshScalar * 2), (int)(MeshScalar * 2), (int)(MeshScalar * 5),faceYZDown:false,
                                                   allMat:BridgeMaterial)
                .ApplyTransforms(Transforms.Translate(0, 0, .5f),
                                 Transforms.Scale(BridgeWidth, BridgeHeight / 2, 1)); // Remove face facing the cylinder
            var bridge2 = MeshShapeGenerator<T>.Cylinder((int)(50 * MeshScalar), angle:pi, upFaceMat:BridgeMaterial
                                                                                         , downFaceMat:BridgeMaterial
                                                                                         , cylinderMat:BridgeMaterial)
                                                    .ApplyTransforms(Transforms.Translate(0, 0, .5f),
                                                                     Transforms.Scale(BridgeWidth / 2, BridgeHeight / 2, 1),
                                                                     Transforms.Translate(0, .5f * BridgeHeight / 2, 0));

            AddColorToMesh(BridgeUpperColor, bridge);
            AddColorToMesh(BridgeLowerColor, bridge2);

            var baseBridge = bridge.Min(x => x.Position.z);
            var fretsAmount = 20;
            var step = BridgeLength / fretsAmount;
            Mesh<T> frets = null;
            for (int i = 0; i < fretsAmount; i++) // Frets
            {
                var fret = MeshShapeGenerator<T>.Box((int)(3 * MeshScalar), FretMaterial).ApplyTransforms(Transforms.Translate(0, 0, .5f),
                                                                                            Transforms.Scale(BridgeWidth, 1, BridgeWidth / 30),
                                                                                            Transforms.Scale(1, i == 0 ? StringBridgeSeparation : BridgeWidth / 30, 1),
                                                                                            Transforms.Translate(0, -.5f * BridgeHeight / 2, -baseBridge + step * i)); // This is not the correct fret spacing
                if (frets == null)
                    frets = fret;
                else
                    frets += fret;
            }
            AddColorToMesh(FretColor, frets);

            // Don't know why but it works when this is done. 
            for (int i = 0; i < bridge.NormalVertex.Length; i++)
            {
                bridge.NormalVertex[i] *= -1;
            }
            // Don't know why but it works when this is done. 
            for (int i = 0; i < frets.NormalVertex.Length; i++)
            {
                frets.NormalVertex[i] *= -1;
            }

            return (bridge + bridge2).ApplyTransforms(Transforms.Scale(1, 1, BridgeLength)) + frets;
        }
    
        public Mesh<T> HeadstockMesh()
        {
            var width = BridgeWidth * 1.3f;
            var height = BridgeHeight / 2.0f;
            var length = BridgeWidth * 2;
            var xHoleScale = 1/5.5f;
            var yHoleScale = 1;
            var zHoleScale = 3 / 4.0f;
            var holeDZ = -length * (1 - zHoleScale) / 2;

            var basePiece = MeshShapeGenerator<T>.Box((int)(4 * MeshScalar), (int)(2 * MeshScalar), (int)(6 * MeshScalar), allMat: HeadstockMaterial)
                                                              .ApplyTransforms(Transforms.Translate(0, 0, -.5f),
                                                                               Transforms.Scale(width, height, length));

            var baseBottom = basePiece.ApplyTransforms(Transforms.Scale(1, 1, (1 - zHoleScale) / 2),
                                                       Transforms.Translate(0, 0, 0));

            var baseTop = baseBottom.ApplyTransforms(Transforms.Translate(0, 0, -length * zHoleScale + holeDZ));

            var middelXScale = xHoleScale*1.5f;
            var baseMiddle = basePiece.ApplyTransforms(Transforms.Scale(middelXScale, 1, zHoleScale),
                                                       Transforms.Translate(0, 0, holeDZ));

            var scaleX = (1 - 3 * xHoleScale) / 2f ; // Last factor is to correct the image
            var dx = width * (1 - xHoleScale) / 2;
            var baseLeft = basePiece.ApplyTransforms(Transforms.Scale(xHoleScale, 1, zHoleScale * 1.1f),
                                                     Transforms.Translate(dx , 0, holeDZ));
            var baseRight = basePiece.ApplyTransforms(Transforms.Scale(xHoleScale, 1, zHoleScale * 1.1f),
                                                      Transforms.Translate(-dx, 0, holeDZ));
            basePiece =
                baseTop
                + baseBottom
                + baseLeft
                + baseMiddle
                + baseRight
                ;

            AddColorToMesh(HeadstockColor, basePiece);

            Mesh<T> stringRollCylinders = null;
            Mesh<T> stringPins = null;
            var step = length * zHoleScale / 3.0f;
            for (int i = 0; i < 6; i++)
            {
                var xBaseCylinderScale = width * xHoleScale;
                var yBaseCylinderScale = height * yHoleScale * .25f;
                var baseCylinder = MeshShapeGenerator<T>.Cylinder((int)(40 * MeshScalar), upFaceMat:   StringCylinderMaterial
                                                                                        , downFaceMat: StringCylinderMaterial
                                                                                        , cylinderMat: StringCylinderMaterial).ApplyTransforms(Transforms.RotateY(pi_over_4 * 2),
                                                                                 Transforms.Scale(xBaseCylinderScale, yBaseCylinderScale, yBaseCylinderScale));

                var zTranslate = ((i % 3)) * step - length - holeDZ + step / 2;
                var yTranslate = 0; //-height / 2.0f;

                baseCylinder = baseCylinder.ApplyTransforms(Transforms.Translate((i < 3 ? 1 : -1) * 1f * (dx - width*xHoleScale), yTranslate, zTranslate));
                for (int j = 0; j < baseCylinder.NormalVertex.Length; j++)
                {
                    baseCylinder.NormalVertex[j] *= -1;
                }

                AddColorToMesh(BaseCylinderColor, baseCylinder);
                if (stringRollCylinders == null)
                    stringRollCylinders = baseCylinder;
                else
                    stringRollCylinders += baseCylinder;

                var xPinScale = xBaseCylinderScale / 2;
                var yPinScale = height * .4f;

                var basePin = MeshShapeGenerator<T>.Cylinder((int)(40 * MeshScalar), upFaceMat:  BasePinMaterial
                                                                                   , downFaceMat:BasePinMaterial 
                                                                                   , cylinderMat:BasePinMaterial 
                                                                        ).ApplyTransforms(Transforms.RotateY(pi_over_4 * 2),
                                                                                          Transforms.Scale(xPinScale, yPinScale, yPinScale));

                for (int j = 0; j < baseCylinder.NormalVertex.Length; j++)
                {
                    basePin.NormalVertex[j] *= -1;
                }
                var yHolderScale = height * 1.5f;
                var xHolderScale = xPinScale / 4;
                var headHolder = MeshShapeGenerator<T>.Cylinder((int)(20 * MeshScalar), upFaceMat:  BasePinMaterial
                                                                                      , downFaceMat:BasePinMaterial 
                                                                                      , cylinderMat:BasePinMaterial 
                                                                        ).ApplyTransforms(Transforms.RotateX(pi_over_4 * 2),
                                                                               Transforms.Translate(0, .5f, 0),
                                                                               Transforms.Scale(xHolderScale, yHolderScale, xHolderScale),
                                                                               Transforms.Translate((i < 3 ? 1 : -1) * xHolderScale, -yPinScale, -xHolderScale * 2));

                var head = MeshShapeGenerator<T>.Cylinder((int)(40 * MeshScalar), upFaceMat:  HeadPinMaterial
                                                                                , downFaceMat:HeadPinMaterial 
                                                                                , cylinderMat:HeadPinMaterial 
                                                                    ).ApplyTransforms(Transforms.RotateX(pi_over_4 * 2),
                                                                         Transforms.Scale(1.1f * xHolderScale, 2.5f * yHolderScale / 6, 2.5f * yHolderScale / 6),
                                                                         Transforms.RotateY((two_pi / 12 * i)),
                                                                         Transforms.Translate((i < 3 ? 1 : -1) * xHolderScale, -yPinScale + yHolderScale, -xHolderScale * 2));

                AddColorToMesh(HeadPinColor, head);
                AddColorToMesh(PinColor, basePin);
                AddColorToMesh(PinColor, headHolder);
                var pin = basePin + headHolder + head;

                pin = pin.ApplyTransforms(Transforms.Translate((i < 3 ? 1 : -1) * (width / 2 + xPinScale / 2), yTranslate, zTranslate));

                if (stringPins == null)
                    stringPins = pin;
                else
                    stringPins += pin;
            }

            // Don't know why but it works when this is done. 
            for (int i = 0; i < basePiece.NormalVertex.Length; i++)
            {
                basePiece.NormalVertex[i] *= -1;
            }

            basePiece += stringRollCylinders + stringPins;
            return basePiece;
        }
    
        public Mesh<T> MainBodyMesh()
        {
            var bodyLength = BridgeLength * 1.1f;

            //Func<float, float2> bazier = BodyFunction();
            //float transform(float x, float val) 
            //{
            //    if (0f <= abs(x) && abs(x) <= .25f && .1f <= abs(val) && abs(val) <= .5f)
            //        return x;
            //    return x * (bazier(val * 2.99f)).y;
            //}; // Multiply val for the max value that bazier is defined, the amount of parts
            //var body = MeshShapeGenerator<T>.Box((int)(15 * MeshScalar),(int)(15 * MeshScalar), (int)(15 * MeshScalar) 
            //                                                  ,holeXZDown:true, sepXZDown:float4(.37f,.61f,.37f,.15f)
            //                                                  )
            //                                    .ApplyTransforms(Transforms.Translate(0, 0, .5f))
            //                                    .Transform<T>(p => new T { Position = float3(transform(p.Position.x, p.Position.z), p.Position.y, p.Position.z) })
            //                                    .ApplyTransforms(Transforms.Translate(0, 0, -.5f));

            var body = GuitarBodyMesh((int)(250 * MeshScalar));
            body = body.Transform(MyTransforms.ExpandInto(body.BoundBox.oppositeCorner, body.BoundBox.topCorner, 1, 1, 1))
                       .Transform(Transforms.Translate(-.5f,-.5f,-.5f));
            
            body = body.ApplyTransforms(Transforms.Translate(0, 0, .5f),
                                        Transforms.Scale(BodyWidth, BridgeHeight * 2, bodyLength),
                                        Transforms.Translate(0, BridgeHeight, BridgeLength - BridgeBodyDif));

            var radius = BridgeWidth * 1.45f / 2;
            var dz = BridgeLength + radius - (BridgeLength / 4 * .2f);
            
            var hole = MeshShapeGenerator<T>.Cylinder((int)(60 * 60 * MeshScalar), thickness: BridgeWidth / 8, surface:true, upFaceMat: GuitarHoleMaterial, downFaceMat: GuitarHoleMaterial, cylinderMat: GuitarHoleMaterial)
                                                   .ApplyTransforms(Transforms.RotateX(pi_over_4 * 2),
                                                                    Transforms.Scale(radius, 1, radius),
                                                                    Transforms.Translate(0, -.6f, dz -.2f));
            for (int i = 0; i < hole.NormalVertex.Length; i++)
            {
                hole.NormalVertex[i] *= -1;
            }
            var stringHub = MeshShapeGenerator<T>.Box((int)(6 * MeshScalar), StringHubMaterial).ApplyTransforms(Transforms.Translate(0, -.5f, .5f),
                                                                                   Transforms.Scale(BridgeWidth, .4f, 1.5f),
                                                                                   Transforms.Translate(0, 0, StringLength));
            stringHub +=    MeshShapeGenerator<T>.Box((int)(6 * MeshScalar), StringHubMaterial).ApplyTransforms(Transforms.Translate(0, -.5f, .5f),
                                                                                   Transforms.Scale(BridgeWidth * 3f, .2f, 1.5f),
                                                                                   Transforms.Translate(0, 0, StringLength));
            stringHub += MeshShapeGenerator<T>.Box((int)(6 * MeshScalar), StringHubTableMaterial).ApplyTransforms(Transforms.Translate(0, -.5f, .5f),
                                                                                Transforms.Scale(BridgeWidth, .6f, .1f),
                                                                                Transforms.Translate(0, 0, StringLength));
            stringHub += MeshShapeGenerator<T>.Box((int)(6 * MeshScalar), StringHubTableMaterial).ApplyTransforms(Transforms.Translate(0, -.5f, .5f),
                                                                                Transforms.Scale(BridgeWidth, .6f, .1f),
                                                                                Transforms.Translate(0, 0, StringLength + .7f));
            stringHub += MeshShapeGenerator<T>.Box((int)(6 * MeshScalar), StringHubTableMaterial).ApplyTransforms(Transforms.Translate(0, -.5f, .5f),
                                                                                Transforms.Scale(BridgeWidth, .6f, .1f),
                                                                                Transforms.Translate(0, 0, StringLength + 1.4f));
            AddColorToMesh(StringHubColor, stringHub);
            AddColorToMesh(BodyColor, body);
            AddColorToMesh(HoleColor, hole);

            // Don't know why but it works when this is done. 
            for (int i = 0; i < stringHub.NormalVertex.Length; i++)
            {
                stringHub.NormalVertex[i] *= -1;
            }

            return hole + body + stringHub;
        }
    
        public Mesh<T> GuitarMesh()
        {
            var body = MainBodyMesh() + BridgeMesh();
            return body + BridgeStringMesh() + HeadstockMesh();
        }

        #endregion

        #region CSG

        public float4x4 CSGWorldTransformation { get; set; } = Transforms.Identity;
        public float3 boxLower = float3(-.5f, -.5f, -.5f);
        public float3 boxUpper = float3(.5f, .5f, .5f);
        public float cylinderRadius = .5f;

        public void BridgeStrings(Scene<T, NoMaterial> scene)
        {
            var strings = new List<(IRaycastGeometry<T>, float4x4, float3)>();
            var step = BridgeWidth / (StringWidths.Length + 1);

            for (int i = 0; i < StringWidths.Length; i++)
            {
                var cylinder = MyRaycaster.Cylinder(.5f
                    , lowerBound: float3(-.5f, -.5f, -.5f)
                    , upperBound: float3(.5f, .5f, .5f));
                var transform = StackTransformations(
                                        Transforms.Translate(0, 0, .5f),
                                        Transforms.Scale(StringWidths[i], StringWidths[i], 1),
                                        Transforms.Translate(-BridgeWidth / 2 + step * (i + 1), -StringBridgeSeparation, 0),
                                        Transforms.Scale(1, 1, StringLength));
                strings.Add((cylinder.AttributesMap(x => new T { Position = x }), transform, ColorToFloat3(StringColors[i])));
            }
            AddToScene(scene, strings);
        }

        public void Bridge(Scene<T, NoMaterial> scene)
        {
            var parts = new List<(IRaycastGeometry<T>, float4x4, float3)>();

            var bridge = MyRaycaster.Box(boxLower, boxUpper);
            var bridgeTransf = StackTransformations(Transforms.Translate(0, 0, .5f),
                                                    Transforms.Scale(BridgeWidth, BridgeHeight / 2, 1));

            var bridge2 = MyRaycaster.Cylinder(cylinderRadius
                    , lowerBound: float3(-.5f, -.5f, -.5f)
                    , upperBound: float3(.5f, .5f, .5f));
            var bridge2Transf = StackTransformations(Transforms.Translate(0, 0, .5f),
                                                     Transforms.Scale(BridgeWidth / 2, BridgeHeight / 2, 1),
                                                     Transforms.Translate(0, .5f * BridgeHeight / 2, 0));

            var baseBridge = min(mul(bridgeTransf, float4x1(boxLower.x, boxLower.y, boxLower.z, 1))._m20, 
                                 mul(bridgeTransf, float4x1(boxUpper.x, boxUpper.y, boxUpper.z, 1))._m20);
            var fretsAmount = 20;
            var step = BridgeLength / fretsAmount;
            var frets = new List<(IRaycastGeometry<T>, float4x4, float3)>();
            for (int i = 0; i < fretsAmount; i++) // Frets
            {
                var fret = MyRaycaster.Box(boxLower, boxUpper);
                var transform = StackTransformations(Transforms.Translate(0, 0, .5f),
                                                     Transforms.Scale(BridgeWidth, 1, BridgeWidth / 30),
                                                     Transforms.Scale(1, i == 0 ? StringBridgeSeparation : BridgeWidth / 30, 1),
                                                     Transforms.Translate(0, -.5f * BridgeHeight / 2, -baseBridge + step * i)); // This is not the correct fret spacing

                frets.Add((fret.AttributesMap(x => new T { Position = x }), transform, ColorToFloat3(FretColor)));
            }
            bridgeTransf  = StackTransformations(bridgeTransf , Transforms.Scale(1, 1, BridgeLength));
            bridge2Transf = StackTransformations(bridge2Transf, Transforms.Scale(1, 1, BridgeLength));
            
            parts.Add((bridge.AttributesMap(x => new T { Position = x }), bridgeTransf, ColorToFloat3(StringHubColor)));
            parts.Add((bridge2.AttributesMap(x => new T { Position = x }), bridge2Transf, ColorToFloat3(StringHubColor)));
            parts.AddRange(frets);
            
            AddToScene(scene, parts);
        }

        public void Headstock(Scene<T, NoMaterial> scene)
        {
            var width = BridgeWidth * 1.3f;
            var height = BridgeHeight / 2.0f;
            var length = BridgeWidth * 2;
            var parts = new List<(IRaycastGeometry<T>, float4x4, float3)>();

            var basePieceTransform = StackTransformations(Transforms.Translate(0, -.5f, -.5f),
                                                          Transforms.Scale(width, height, length));
            var basePiece = new CSGNode(MyRaycaster.Box(boxLower, boxUpper), basePieceTransform); 
            var xHoleScale = 3 / 16.0f;
            var yHoleScale = 1;
            var zHoleScale = 3 / 4.0f;
            var holeDZ = -length * (1 - zHoleScale) / 2;

            var hole1Transf = StackTransformations(basePieceTransform,
                                                  Transforms.Scale(xHoleScale, yHoleScale * 2f, zHoleScale),
                                                  Transforms.Translate(width * xHoleScale, height / 2, holeDZ));
            var hole1 = new CSGNode(MyRaycaster.Box(boxLower, boxUpper), hole1Transf);
            
            var hole2Transf = StackTransformations(hole1Transf,
                                                   Transforms.Translate(-2 * width * xHoleScale, 0, 0));
            var hole2 = new CSGNode(MyRaycaster.Box(boxLower, boxUpper), hole2Transf);

            basePiece = basePiece / (hole1 | hole2);

            parts.Add((basePiece.AttributesMap(x => new T { Position = x }), Transforms.Identity, ColorToFloat3(HeadstockColor))); // Identity because the transformation is already in CSGNode

            var stringRollCylinders = new List<(IRaycastGeometry<T>, float4x4, float3)>();
            var stringPins = new List<(IRaycastGeometry<T>, float4x4, float3)>();
            var step = length * zHoleScale / 3.0f;
            for (int i = 0; i < 6; i++)
            {
                var xBaseCylinderScale = width * xHoleScale;

                var zTranslate = ((i % 3)) * step - length - holeDZ + step / 2;
                var yTranslate = -height / 2.0f;
                var baseCylinder = MyRaycaster.Cylinder(cylinderRadius
                    , lowerBound: float3(-.5f, -.5f, -.5f)
                    , upperBound: float3(.5f, .5f, .5f));
                var baseCylinderTransf = StackTransformations(Transforms.RotateY(pi_over_4 * 2),
                                                              Transforms.Scale(xBaseCylinderScale, height * yHoleScale * .25f, height * yHoleScale * .25f),
                                                              Transforms.Translate((i < 3 ? 1 : -1) * 1f * (width * xHoleScale), yTranslate, zTranslate));
                stringRollCylinders.Add((baseCylinder.AttributesMap(x => new T { Position = x }), baseCylinderTransf, ColorToFloat3(HeadPinColor)));


                var xPinScale = xBaseCylinderScale / 3;
                var yPinScale = height * .2f;
                var finalPosTransf = Transforms.Translate((i < 3 ? 1 : -1) * (width / 2 + xPinScale / 2), yTranslate, zTranslate);


                var basePin = MyRaycaster.Cylinder(cylinderRadius
                    , lowerBound: float3(-.5f, -.5f, -.5f)
                    , upperBound: float3(.5f, .5f, .5f));
                var basePinTrans = StackTransformations(Transforms.RotateY(pi_over_4 * 2),
                                                        Transforms.Scale(xPinScale, yPinScale, yPinScale),
                                                        finalPosTransf);

                var yHolderScale = height * 1.5f;
                var xHolderScale = xPinScale / 4;
                var headHolder = MyRaycaster.Cylinder(cylinderRadius
                    , lowerBound: float3(-.5f, -.5f, -.5f)
                    , upperBound: float3(.5f, .5f, .5f));
                var headHolderTransf = StackTransformations(Transforms.RotateX(pi_over_4 * 2),
                                                            Transforms.Translate(0, .5f, 0),
                                                            Transforms.Scale(xHolderScale, yHolderScale, xHolderScale),
                                                            Transforms.Translate((i < 3 ? 1 : -1) * xHolderScale, -yPinScale, -xHolderScale * 2),
                                                            finalPosTransf);

                var head = MyRaycaster.Cylinder(cylinderRadius
                    , lowerBound: float3(-.5f, -.5f, -.5f)
                    , upperBound: float3(.5f, .5f, .5f));
                var headTransf = StackTransformations(Transforms.RotateX(pi_over_4 * 2),
                                                      Transforms.Scale(1.1f * xHolderScale, 2 * yHolderScale / 6, 2 * yHolderScale / 3),
                                                      Transforms.RotateY((two_pi / 12 * i)),
                                                      Transforms.Translate((i < 3 ? 1 : -1) * xHolderScale, -yPinScale + yHolderScale, -xHolderScale * 2),
                                                      finalPosTransf);

                stringPins.Add((basePin.AttributesMap(x => new T { Position = x }), basePinTrans, ColorToFloat3(BaseCylinderColor)));
                stringPins.Add((headHolder.AttributesMap(x => new T { Position = x }), headHolderTransf, ColorToFloat3(BaseCylinderColor)));
                stringPins.Add((head.AttributesMap(x => new T { Position = x }), headTransf, ColorToFloat3(HeadPinColor)));
            }
            parts.AddRange(stringRollCylinders);
            parts.AddRange(stringPins);

            AddToScene(scene, parts);
        }

        public void MainBody(Scene<T, NoMaterial> scene)
        {
            var parts = new List<(IRaycastGeometry<T>, float4x4, float3)>();
            
            var bodyLength = BridgeLength * 1.1f;

            var bodyTransf = StackTransformations(Transforms.Translate(0, 0, .5f),
                                                  Transforms.Scale(BodyWidth, BridgeHeight * 2, bodyLength),
                                                  Transforms.Translate(0, BridgeHeight, BridgeLength - BridgeBodyDif));
            var body = new CSGNode(MyRaycaster.Box(boxLower, boxUpper), bodyTransf);
            var innerBody = new CSGNode(MyRaycaster.Box(boxLower + .1f, boxUpper - .1f), bodyTransf);


            var radius = BridgeWidth * 1.5f ;
            var dz = BridgeLength + radius - (BridgeLength / 4 * .2f);
            var cTransf = StackTransformations(Transforms.RotateX(pi_over_4 * 2),
                                               Transforms.Scale(radius, 2, radius),
                                               Transforms.Translate(0, -.25f, dz));
            var c = new CSGNode(MyRaycaster.Cylinder(cylinderRadius
                    , lowerBound: float3(-.5f, -.5f, -.5f)
                    , upperBound: float3(.5f, .5f, .5f)), cTransf);

            var holeTransf = StackTransformations(Transforms.RotateX(pi_over_4 * 2),
                                                  Transforms.Scale(radius, .01f, radius),
                                                  Transforms.Translate(0, 0, dz));
            var hole = new CSGNode(MyRaycaster.Cylinder(cylinderRadius
                    , lowerBound: float3(-.5f, -.5f, -.5f)
                    , upperBound: float3(.5f, .5f, .5f)), holeTransf);

            body = (body | hole) / c;
            body /= innerBody;

            parts.Add((body.AttributesMap(x => new T { Position = x }), Transforms.Identity, ColorToFloat3(HeadstockColor)));

            var stringHub = MyRaycaster.Box(boxLower, boxUpper);
            var stringHubTransf = StackTransformations(Transforms.Translate(0, -.5f, .5f),
                                                       Transforms.Scale(BridgeWidth, 1, 1.5f),
                                                       Transforms.Translate(0, 0, StringLength));
            var stringHub2 = MyRaycaster.Box(boxLower, boxUpper);
            var stringHub2Transf = StackTransformations(Transforms.Translate(0, -.5f, .5f),
                                                        Transforms.Scale(BridgeWidth * 3f, .5f, 1.5f),
                                                        Transforms.Translate(0, 0, StringLength));
            parts.Add((stringHub.AttributesMap(x => new T { Position = x }), stringHubTransf, ColorToFloat3(BaseCylinderColor)));
            parts.Add((stringHub2.AttributesMap(x => new T { Position = x }), stringHub2Transf, ColorToFloat3(BaseCylinderColor)));

            AddToScene(scene, parts);
        }

        private float3 ColorToFloat3(Color bodyColor)
        {
            return float3(bodyColor.R / 255.0f, bodyColor.G / 255.0f, bodyColor.B / 255.0f);
        }

        public void Guitar(Scene<T, NoMaterial> scene)
        {
            BridgeStrings(scene);
            Bridge(scene);
            Headstock(scene);
            MainBody(scene);
        }
        #endregion

        #region Utils

        /// <summary>
        /// Guitar parametric shape function, defined between 0 and 3, 
        /// initial: (0,0) 
        /// max y: (2,1) 
        /// max x: (2.77,0) 
        /// y >= 0 
        /// x >=0
        /// </summary>
        /// <returns></returns>
        public static Func<float, float2> BodyFunction()
        {
            var bazierPoints = new List<float2>
            {
                float2(0,0),
                float2(0,0.965f),
                float2(0.8498f,0.746f),
                
                float2(0.85f,0.7f),
 
                float2(0.972f,0.642f),
                float2(1.205f,0.66f),
                
                float2(1.315f,0.736f),

                float2(1.39f,0.88f),
                float2(2.77f,1.52f),
                float2(2.77f,0f),
                //float2(2.5f,1.51f),
                //float2(2.5f,0f),
            };
            return PartFunction(BazierCurve(bazierPoints.GetRange(0, 4).ToArray()),
                                BazierCurve(bazierPoints.GetRange(3, 4).ToArray()),
                                BazierCurve(bazierPoints.GetRange(6, 4).ToArray()));
        }

        // Chops the funcs evaluation in intervals of 1
        public static Func<float, float2> PartFunction(params Func<float, float2>[] funcs)
        {
            return t =>
            {
                var index = (int)Math.Floor(t);
                if (funcs.Length <= index || index < 0)
                    throw new ArgumentException("Function is not defined");
                return funcs[index](t - index);
            };
        }

        public static Func<float, float2> BazierCurve(params float2[] points)
        {
            return BazierCurve(points[0], points[1], points[2], points[3]);
        }

        public static Func<float, float2> BazierCurve(float2 p1, float2 p2, float2 p3, float2 p4)
        {
            return t => float2(
            (1 - t) * ((1 - t) * ((1 - t) * p1.x
            + t * p2.x) + t * ((1 - t) * p2.x
            + t * p3.x)) + t * ((1 - t) * ((1 - t) * p2.x
            + t * p3.x) + t * ((1 - t) * p3.x
            + t * p4.x)),
            (1 - t) * ((1 - t) * ((1 - t) * p1.y
            + t * p2.y) + t * ((1 - t) * p2.y
            + t * p3.y)) + t * ((1 - t) * ((1 - t) * p2.y
            + t * p3.y) + t * ((1 - t) * p3.y
            + t * p4.y)));
        }
    
        public float4x4 StackTransformations(params float4x4[] transformations)
        {
            var transform = Transforms.Identity;
            foreach (var item in transformations)
            {
                transform = mul(transform, item);
            }
            return transform;
        }

        public void AddToScene(Scene<T, NoMaterial> scene, IEnumerable<(IRaycastGeometry<T>, float4x4, float3)> geometries)
        {
            foreach (var (geo, trans, color) in geometries)
            {
                scene.Add(geo, new NoMaterial() { Color = color }, mul(trans, CSGWorldTransformation));
            }
        }
    
        public static void AddColorToMesh(Color color, Mesh<T> mesh)
        {
            static float3 Color2Float3(Color color)
            {
                return float3(color.R / 255.0f, color.B / 255.0f, color.G / 255.0f);
            }

            for (int i = 0; i < mesh.Vertices.Length; i++)
            {
                mesh.Vertices[i].SetColor(color);
            }
        }

        public static Mesh<T> GuitarBodyMesh(int pointsLength)
        {
            float step = 3.0f / pointsLength;
            float deep = 5f;

            Mesh<T> frontBody = null,
                    backBody = null,
                    sideBody = null;

            var bz = BodyFunction();
            
            static float holeFunc(float x)
            {
                float centerX = .75f;
                float radiusSqr = .3f * .3f;
                var sqrtInside = radiusSqr - (x - centerX) * (x - centerX);
                return sqrtInside < 0 ? .0f : sqrt(sqrtInside); // 0.001 returned because of weird bug in shading when both parts are joined. Without it, should be 0
            }
            
            var backMaterial = GuitarBodyBackMaterial;
            var frontMaterial = GuitarBodyFrontMaterial;
            var sideMaterial = GuitarBodySidesMaterial;
            
            var prevLRBZ = bz(step);
            for (int i = 0; (i+1) * step < 3; i++)
            {
                var t0 = step * i;
                var t1 = step * (i + 1);
                var prev = bz(t0);
                var curr = bz(t1);
                // LL => Lower Left
                // UR => Upper Right
                // Up, Down => Up face side
                float3 LLUp = float3(0, deep, prev.x), ULUp = float3( prev.y, deep, prev.x),
                       LRUp = float3(0, deep, curr.x), URUp = float3( curr.y, deep, curr.x);
                float3 LLDw = float3(holeFunc(prev.x), 0, prev.x), ULDw = float3( prev.y, 0, prev.x),
                       LRDw = float3(holeFunc(curr.x), 0, curr.x), URDw = float3( curr.y, 0, curr.x);

                // Corner Auxiliar Vertex, to prevent normal issues
                float3 auxULUp1 = ULUp,
                       auxURUp1 = URUp,
                       auxULDw1 = ULDw,
                       auxURDw1 = URDw;

                float3 auxULUp2 = ULUp - 0.05f * (ULUp - LLUp),
                       auxURUp2 = URUp - 0.05f * (URUp - LRUp),
                       auxULDw2 = ULDw - 0.05f * (ULDw - LLDw),
                       auxURDw2 = URDw - 0.05f * (URDw - LRDw);

                var panel1 = 
                    new Mesh<T>(
                        new T[]
                    {
                        new T() //0
                        {
                            Position = ULDw
                        },
                        new T() //1
                        {
                            Position = ULUp
                        },
                        new T() //2
                        {
                            Position = URDw
                        },
                        new T() //3
                        {
                            Position = URUp
                        },
                        new T() // 9
                        {
                            Position = auxULUp1
                        },
                        new T() // 11
                        {
                            Position = auxURUp1
                        },
                        new T() // 12
                        {
                            Position = auxULUp2
                        },
                        new T() // 14
                        {
                            Position = auxURUp2
                        },
                        new T() // 4
                        {
                            Position = LLUp
                        },
                        new T() // 5
                        {
                            Position = LRUp
                        },
                        new T() // 8
                        {
                            Position = auxULDw1
                        },
                        new T() // 10
                        {
                            Position = auxURDw1
                        },
                        new T() // 13
                        {
                            Position = auxULDw2
                        },
                        new T() // 15
                        {
                            Position = auxURDw2
                        },
                        new T() // 6
                        {
                            Position = LLDw
                        },
                        new T() // 7
                        {
                            Position = LRDw
                        },
                    },
                    new int[] 
                    {
                        0,1,2,
                        1,3,2,
                        4,6,5,
                        6,8,7,
                        8,9,7,
                        6,7,5,
                        10,12,11,
                        12,14,13,
                        14,15,13,
                        12,13,11,
                    });

                panel1.Materials = new IMaterial[]
                {
                    sideMaterial,
                    backMaterial,
                    frontMaterial,
                };
                panel1.MaterialsSeparators = new int[]
                {
                    4,
                    10,
                    16,
                };
                panel1.NormalVertex = new float3[]
                {
                    float3(1,0,0),
                    float3(0,1,0),
                    float3(0,1,0),
                };
                panel1.NormalSeparators = new int[]
                {
                    4,
                    10,
                    16,
                };

                var panel2 = panel1.Transform<T>(x => new T() { Position = float3(-x.Position.x - .0f, x.Position.y, x.Position.z) });
                panel2.NormalVertex = new float3[]
                {
                    float3(1,0,0),
                    float3(0,1,0),
                    float3(0,1,0),
                };
                panel2.NormalSeparators = new int[]
                {
                    4,
                    10,
                    16,
                };

                var bodyParts = panel1.MaterialDecompose().ToArray();
                for (int k = 0; k < bodyParts.Length; k++)
                {
                    switch (k)
                    {
                        case 0: // side
                            sideBody = sideBody == null ? bodyParts[k] : sideBody + bodyParts[k];
                            break;
                        case 1: // back
                            backBody = backBody == null ? bodyParts[k] : backBody + bodyParts[k];
                            break;
                        case 2: // front
                            frontBody = frontBody == null ? bodyParts[k] : frontBody + bodyParts[k];
                            break;
                        default:
                            break;
                    }
                }
                bodyParts = panel2.MaterialDecompose().ToArray();
                for (int k = 0; k < bodyParts.Length; k++)
                {
                    switch (k)
                    {
                        case 0: // side
                            sideBody = sideBody == null ? bodyParts[k] : sideBody + bodyParts[k];
                            break;
                        case 1: // back
                            backBody = backBody == null ? bodyParts[k] : backBody + bodyParts[k];
                            break;
                        case 2: // front
                            frontBody = frontBody == null ? bodyParts[k] : frontBody + bodyParts[k];
                            break;
                        default:
                            break;
                    }
                }
            }
            // All this material shit is for Memory Usage, only one material instance is used. Also it improves time performance
            frontBody.SetMaterial(frontMaterial);
            frontBody = MaterialsUtils.MapPlane(frontBody);

            backBody.SetMaterial(backMaterial);
            backBody = MaterialsUtils.MapPlane(backBody);

            sideBody.SetMaterial(sideMaterial);
            sideBody = MaterialsUtils.MapPlane(sideBody);

            var body = sideBody + frontBody + backBody;
            return body;
        }

        #endregion
    }
}
