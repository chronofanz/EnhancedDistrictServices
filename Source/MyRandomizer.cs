using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using static TransferManager;

namespace EnhancedDistrictServices
{
    public class MyRandomizer
    {
        public ulong seed;

        public MyRandomizer(int _seed) => this.seed = (ulong)(6364136223846793005L * (long)_seed + 1442695040888963407L);

        public MyRandomizer(uint _seed) => this.seed = (ulong)(6364136223846793005L * (long)_seed + 1442695040888963407L);

        public MyRandomizer(long _seed) => this.seed = (ulong)_seed;

        public MyRandomizer(ulong _seed) => this.seed = _seed;

        public int Bits32(int num)
        {
            int num1 = (int)(this.seed >> 64 - num);
            this.seed = (ulong)(6364136223846793005L * (long)this.seed + 1442695040888963407L);
            return num1;
        }

        public int Int32(uint range)
        {
            int num = (int)((this.seed >> 32) * (ulong)range >> 32);
            this.seed = (ulong)(6364136223846793005L * (long)this.seed + 1442695040888963407L);
            return num;
        }

        public int Int32(int min, int max)
        {
            int num = min + (int)((this.seed >> 32) * (ulong)(uint)(max - min + 1) >> 32);
            this.seed = (ulong)(6364136223846793005L * (long)this.seed + 1442695040888963407L);
            return num;
        }

        public uint UInt32(uint range)
        {
            uint num = (uint)((this.seed >> 32) * (ulong)range >> 32);
            this.seed = (ulong)(6364136223846793005L * (long)this.seed + 1442695040888963407L);
            return num;
        }

        public uint UInt32(uint min, uint max)
        {
            uint num = min + (uint)((this.seed >> 32) * (ulong)(uint)((int)max - (int)min + 1) >> 32);
            this.seed = (ulong)(6364136223846793005L * (long)this.seed + 1442695040888963407L);
            return num;
        }

        public ulong ULong64()
        {
            ulong seed = this.seed;
            this.seed = (ulong)(6364136223846793005L * (long)this.seed + 1442695040888963407L);
            return seed;
        }
    }
}
