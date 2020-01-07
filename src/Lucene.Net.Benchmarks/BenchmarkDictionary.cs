using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lucene.Net.Benchmarks
{
    [MemoryDiagnoser]
    public class BenchmarkDictionary
    {
        private static J2N.Collections.Generic.Dictionary<string, string> dictionary = new J2N.Collections.Generic.Dictionary<string, string>
        {
            ["one"] = "1",
            ["two"] = "2",
            ["three"] = "3",
            ["four"] = "4",
            ["five"] = "5",
            ["six"] = "6",
            ["seven"] = "7",
            ["eight"] = "8",
            ["nine"] = "9",
            ["ten"] = "10",
            [null] = "theNull",

            //[1] = "1",
            //[2] = "2",
            //[3] = "3",
            //[4] = "4",
            //[5] = "5",
            //[6] = "6",
            //[7] = "7",
            //[8] = "8",
            //[9] = "9",
            //[10] = "10",
            ////[null] = "theNull",
        };

        private static Lucene.Net.Support.HashMap<string, string> hashmap = new Support.HashMap<string, string>
        {
            ["one"] = "1",
            ["two"] = "2",
            ["three"] = "3",
            ["four"] = "4",
            ["five"] = "5",
            ["six"] = "6",
            ["seven"] = "7",
            ["eight"] = "8",
            ["nine"] = "9",
            ["ten"] = "10",
            [null] = "theNull",

            //[1] = "1",
            //[2] = "2",
            //[3] = "3",
            //[4] = "4",
            //[5] = "5",
            //[6] = "6",
            //[7] = "7",
            //[8] = "8",
            //[9] = "9",
            //[10] = "10",
            ////[null] = "theNull",
        };



        //[Benchmark]
        //public void Dictionary_Add() => dictionary.Add("foo", "bar");

        //[IterationCleanup(Target = nameof(Dictionary_Add))]
        //public void Dictionary_Add_Cleanup() => dictionary.Remove("foo");

        //[Benchmark]
        //public void HashMap_Add() => hashmap.Add("foo", "bar");

        //[IterationCleanup(Target = nameof(HashMap_Add))]
        //public void HashMap_Add_Cleanup() => hashmap.Remove("foo");



        //[IterationSetup(Target = nameof(Dictionary_Remove))]
        //public void Dictionary_Remove_Setup() => dictionary.Add("ToRemove", "bogus");

        //[Benchmark]
        //public void Dictionary_Remove() => dictionary.Remove("ToRemove");

        //[IterationSetup(Target = nameof(HashMap_Remove))]
        //public void HashMap_Remove_Setup() => hashmap.Add("ToRemove", "bogus");

        //[Benchmark]
        //public void HashMap_Remove() => hashmap.Remove("ToRemove");


        //[Benchmark]
        //public void Dictionary_Update() => dictionary["four"] = "six";

        //[IterationCleanup(Target = nameof(Dictionary_Update))]
        //public void Dictionary_Update_Cleanup() => dictionary["four"] = "4";

        ////[Benchmark]
        ////public void Dictionary_Update2() => dictionary.Update("four", "six");

        ////[IterationCleanup(Target = nameof(Dictionary_Update2))]
        ////public void Dictionary_Update2_Cleanup() => dictionary["four"] = "4";


        //[Benchmark]
        //public void HashMap_Update() => hashmap["four"] = "six";

        //[IterationCleanup(Target = nameof(HashMap_Update))]
        //public void HashMap_Update_Cleanup() => hashmap["four"] = "4";


        [Benchmark]
        public void Dictionary_Update() => dictionary["four"] = "six";

        [IterationCleanup(Target = nameof(Dictionary_Update))]
        public void Dictionary_Update_Cleanup() => dictionary["four"] = "4";

        //[Benchmark]
        //public void Dictionary_Update2() => dictionary.Update("four", "six");

        //[IterationCleanup(Target = nameof(Dictionary_Update2))]
        //public void Dictionary_Update2_Cleanup() => dictionary["four"] = "4";


        [Benchmark]
        public void HashMap_Update() => hashmap["four"] = "six";

        [IterationCleanup(Target = nameof(HashMap_Update))]
        public void HashMap_Update_Cleanup() => hashmap["four"] = "4";


        [Benchmark]
        public void Dictionary_IterateKVP()
        {
            foreach (var pair in dictionary)
            {

            }
            foreach (var pair in dictionary)
            {

            }
            foreach (var pair in dictionary)
            {

            }
        }

        [Benchmark]
        public void HashMap_IterateKVP()
        {
            foreach (var pair in hashmap)
            {

            }
            foreach (var pair in hashmap)
            {

            }
            foreach (var pair in hashmap)
            {

            }
        }


        [Benchmark]
        public void Dictionary_IterateKeys()
        {
            foreach (var pair in dictionary.Keys)
            {

            }
            foreach (var pair in dictionary.Keys)
            {

            }
            foreach (var pair in dictionary.Keys)
            {

            }
        }

        [Benchmark]
        public void HashMap_IterateKeys()
        {
            foreach (var pair in hashmap.Keys)
            {

            }
            foreach (var pair in hashmap.Keys)
            {

            }
            foreach (var pair in hashmap.Keys)
            {

            }
        }


        [Benchmark]
        public void Dictionary_IterateValues()
        {
            foreach (var pair in dictionary.Values)
            {

            }
            foreach (var pair in dictionary.Values)
            {

            }
            foreach (var pair in dictionary.Values)
            {

            }
        }

        [Benchmark]
        public void HashMap_IterateValues()
        {
            foreach (var pair in hashmap.Values)
            {

            }
            foreach (var pair in hashmap.Values)
            {

            }
            foreach (var pair in hashmap.Values)
            {

            }
        }

    }
}
