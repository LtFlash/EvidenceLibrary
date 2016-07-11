﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;

namespace EvidenceLibrary
{
    public struct SpawnPoint
    {
        public float Heading;
        public Vector3 Position;
        public SpawnPoint(float Heading, Vector3 Position)
        {
            this.Heading = Heading;
            this.Position = Position;
        }
        public static SpawnPoint Zero
        {
            get
            {
                return new SpawnPoint(0.0f, Vector3.Zero);
            }
        }
    };
}