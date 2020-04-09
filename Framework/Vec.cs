﻿using System;
using System.IO;

namespace HackFramework {
    public struct Vec3 {
        public        float X, Y, Z;
        public static Vec3  Zero => new Vec3( 0, 0, 0 );

        #region Overrides of ValueType

        /// <inheritdoc />
        public override string ToString() => $"{{{this.X},{this.Y},{this.Z}}}";

        #endregion

        public Vec3(float x, float y, float z) {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public Vec3(Vec2 v, float z) {
            this.X = v.X;
            this.Y = v.Y;
            this.Z = z;
        }

        public Vec3(double x, double y, double z) {
            this.X = (float) x;
            this.Y = (float) y;
            this.Z = (float) z;
        }

        public static float Distance(Vec3 a, Vec3 b) {
            float dx = b.X - a.X;
            float dy = b.Y - a.Y;
            float dz = b.Z - a.Z;
            return (float) Math.Sqrt( dx * dx + dy * dy + dz * dz );
        }

        public float DistanceTo(Vec3 a) { return Distance( a, this ); }

        public Vec3 Add(Vec3 vec3) { return new Vec3( this.X + vec3.X, this.Y + vec3.Y, this.Z + vec3.Z ); }
    }

    public struct Vec2 {
        public float X, Y;

        #region Overrides of ValueType

        /// <inheritdoc />
        public override string ToString() => $"{{{this.X},{this.Y}}}";

        #endregion

        public Vec2(float x, float y) {
            this.X = x;
            this.Y = y;
        }
    }
}