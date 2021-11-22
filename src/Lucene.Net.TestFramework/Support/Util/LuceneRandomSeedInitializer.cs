﻿using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using System;
using System.Linq;
using System.Reflection;

namespace Lucene.Net.Util
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    internal class LuceneRandomSeedInitializer
    {
        #region Messages

        const string RANDOM_SEED_PARAMS_MSG =
            "\"tests:seed\" parameter must be a valid long hexadecimal value or the word \"random\".";

        #endregion

        private J2N.Randomizer random;
        private long initialSeed;

        /// <summary>
        /// Tries to get the random seed from either a <see cref="RandomSeedAttribute"/> or the "tests:seed" system property.
        /// If niether of these exist, a random seed will be generated and this method returns <c>false</c>;
        /// </summary>
        /// <param name="test">The test fixture.</param>
        /// <param name="seed">The random seed for a new <see cref="Random"/> instance.
        /// Note this is a subclass of <see cref="Random"/>, since the default doesn't produce consistent results across platforms.</param>
        /// <returns><c>true</c> if the seed was found in context; <c>false</c> if the seed was generated.</returns>
        private bool TryGetRandomSeedFromContext(Test test, out long seed)
        {
            bool generate;
            seed = 0;
            var randomSeedAttribute = (RandomSeedAttribute)test.TypeInfo.Assembly
                .GetCustomAttributes(typeof(RandomSeedAttribute), inherit: false)
                .FirstOrDefault();
            if (randomSeedAttribute != null)
            {
                seed = randomSeedAttribute.RandomSeed;
                generate = false;
            }
            else
            {
                // For now, ignore anything NUnit3TestAdapter does, because it is messing up repeatable runs.
                var seedAsString = SystemProperties.GetProperty("tests:seed", null);
                if (seedAsString is null || "random".Equals(seedAsString, StringComparison.OrdinalIgnoreCase))
                {
                    generate = true;
                }
                else if (J2N.Numerics.Int64.TryParse(seedAsString, 16, out seed))
                {
                    generate = false;
                }
                else
                {
                    generate = true;
                    test.MakeInvalid(RANDOM_SEED_PARAMS_MSG);
                }
            }

            if (generate)
            {
                seed = new J2N.Randomizer().NextInt64();
                return false;
            }
            return true;
        }

        /// <summary>
        /// Initializes the randomized context and seed for the test fixture.
        /// </summary>
        /// <param name="fixture">The test fixture.</param>
        /// <param name="seedOffset">Offset that will be added to the initial seed. This should be different for SetUpFixture and TestFixture attributes
        /// so they have different seeds that are deterministically based on the initial seed.</param>
        /// <returns>The randomized context.</returns>
        public RandomizedContext InitializeTestFixture(Test fixture, Assembly testAssembly, int seedOffset = 0)
        {
            if (fixture is null)
                throw new ArgumentNullException(nameof(fixture));

            TryGetRandomSeedFromContext(fixture, out initialSeed); // NOTE: This sets the initialSeed field for this class.
            random = new J2N.Randomizer(initialSeed + seedOffset);

            int goodFastHashSeed = (int)initialSeed * 31; // LUCENENET: Multiplying 31 to remove the possility of a collision with the test framework while still using a deterministic number.
            if (StringHelper.goodFastHashSeed != goodFastHashSeed)
                StringHelper.goodFastHashSeed = goodFastHashSeed;

            // Now we need to generate the first seed for our test fixture
            // which will be used during OneTimeSetUp and OneTimeTearDown.
            // Assumption: The passed in fixture doesn't have any tests added.
            // The tests are added in a later step to prevent differences in the
            // result when there are filters applied.

            // Generate a new long value that is the seed for this specific test.
            return InitializeTestFixture(fixture, new RandomizedContext(fixture, testAssembly, initialSeed, random.NextInt64()));
        }

        /// <summary>
        /// Initializes the randomized context for the fixture.
        /// </summary>
        /// <param name="fixture">The test fixture.</param>
        /// <param name="randomizedContext">The randomized context to associate with the fixture.</param>
        /// <returns>The randomized context.</returns>
        public RandomizedContext InitializeTestFixture(Test fixture, RandomizedContext randomizedContext)
        {
            if (fixture is null)
                throw new ArgumentNullException(nameof(fixture));

            fixture.Properties.Set(RandomizedContext.RandomizedContextPropertyName, randomizedContext);
            return randomizedContext;
        }

        /// <summary>
        /// Generates random seeds for the given test and all of its children that
        /// can be cast to <see cref="Test"/>.
        /// </summary>
        /// <param name="test"></param>
        public void GenerateRandomSeeds(Test test)
        {
            SetRandomSeeds(test);
        }

        private void SetRandomSeeds(Test test)
        {
            if (test is null)
                return;

            var testAssembly = test is ParameterizedMethodSuite ? test.Tests[0].TypeInfo.Assembly : test.TypeInfo.Assembly;

            test.Properties.Set(
                RandomizedContext.RandomizedContextPropertyName,
                // Generate a new long value that is the seed for this specific test.
                new RandomizedContext(test, testAssembly, initialSeed, random.NextInt64()));

            if (test.HasChildren)
            {
                foreach (ITest child in test.Tests)
                    if (child is Test testChild)
                        SetRandomSeeds(testChild);
            }
        }
    }
}
