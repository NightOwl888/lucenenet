using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lucene.Net.Benchmarks
{
    [MemoryDiagnoser]
    public class BenchmarkCharacter
    {
        //[Benchmark]
        //public char[] ToChars_Support() => Support.Character.ToChars(183);

        //[Benchmark]
        //public char[] ToChars_J2N() => J2N.Character.ToChars(183);

        //[Benchmark]
        //public int ToChars_2_Support()
        //{
        //    char[] chars = new char[2];
        //    return Support.Character.ToChars(183, chars, 0);
        //}

        //[Benchmark]
        //public int ToChars_2_J2N()
        //{
        //    char[] chars = new char[2];
        //    return J2N.Character.ToChars(183, chars, 0);
        //}


        [Benchmark]
        public int ToUpper_Support() => Support.Character.ToUpper(183);

        //[Benchmark]
        //public int ToUpper2_Support() => Support.Character.ToUpper2(183);

        //[Benchmark]
        //public int ToUpper3_Support() => Support.Character.ToUpper3(183);


        [Benchmark]
        public int ToLower_Support() => Support.Character.ToLower(183);

        //[Benchmark]
        //public int ToLower2_Support() => Support.Character.ToLower2(183);

        //[Benchmark]
        //public int ToLower3_Support() => Support.Character.ToLower3(183);



        [Benchmark]
        public int ToUpper_SurrogatePair_Support() => Support.Character.ToUpper(119070);

        //[Benchmark]
        //public int ToUpper2_SurrogatePair_Support() => Support.Character.ToUpper2(119070);

        //[Benchmark]
        //public int ToUpper3_SurrogatePair_Support() => Support.Character.ToUpper3(119070);


        [Benchmark]
        public int ToLower_SurrogatePair_Support() => Support.Character.ToLower(119070);

        //[Benchmark]
        //public int ToLower2_SurrogatePair_Support() => Support.Character.ToLower2(119070);

        //[Benchmark]
        //public int ToLower3_SurrogatePair_Support() => Support.Character.ToLower3(119070);


        //119070
    }
}
