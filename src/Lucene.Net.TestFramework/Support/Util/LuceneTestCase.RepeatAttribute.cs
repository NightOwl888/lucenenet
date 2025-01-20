using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal.Commands;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#nullable enable

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

    public abstract partial class LuceneTestCase
    {
        [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
        public class RepeatAttribute : System.Attribute, IWrapSetUpTearDown
        {
            private readonly int repeatCount;

            public RepeatAttribute(int repeatCount)
            {
                this.repeatCount = repeatCount;
            }

            public TestCommand Wrap(TestCommand command)
            {
                return new TestRepeatCommand(command, repeatCount);
            }

            private class TestRepeatCommand : DelegatingTestCommand
            {
                private readonly int repeatCount;

                public TestRepeatCommand(TestCommand innerCommand, int repeatCount)
                    : base(innerCommand)
                {
                    this.repeatCount = repeatCount;
                }

                private static RandomizedContext? GetRandomizedContext(ITest? test)
                {
                    if (test is null)
                        return null;

                    if (test.Properties.ContainsKey(RandomizedContext.RandomizedContextPropertyName))
                        return (RandomizedContext?)test.Properties.Get(RandomizedContext.RandomizedContextPropertyName);

                    return null;
                }

                public override TestResult Execute(TestExecutionContext context)
                {
                    TestResult result = context.CurrentResult;
                    Test currentTest = context.CurrentTest;
                    RandomizedContext? randomizedContext = GetRandomizedContext(currentTest);  // context.CurrentTest.GetRandomizedContext();
                    if (randomizedContext is null)
                    {
                        // LUCENENET TODO: We should report an error here, since this attribute is not allowed outside of the test framework.
                        // For now, we are just skipping this attribute.
                        return innerCommand.Execute(context);
                    }

                    var repeatRandom = new J2N.Randomizer(randomizedContext.RandomSeed);

                    for (int i = 0; i < repeatCount; i++)
                    {
                        Console.WriteLine($"Iteration '{i}'");
                        // Regenerate the random seed for this iteration
                        long testSeed = repeatRandom.NextInt64();
                        randomizedContext.ResetRandom(testSeed);

                        result = innerCommand.Execute(context);

                        if (result.ResultState == ResultState.Failure || result.ResultState == ResultState.Error)
                        {
                            string message = $"Repeat failed on iteration '{i}'.{Environment.NewLine}{Environment.NewLine}{result.Message}";
                            result.SetResult(result.ResultState, message, result.StackTrace);
                            break;
                        }
                    }

                    return result;
                }
            }

            private class FullRepeatCommand : DelegatingTestCommand
            {
                private readonly int repeatCount;

                public FullRepeatCommand(TestCommand innerCommand, int repeatCount)
                    : base(innerCommand)
                {
                    this.repeatCount = repeatCount;
                }

                private static RandomizedContext? GetRandomizedContext(ITest? test)
                {
                    if (test is null)
                        return null;

                    if (test.Properties.ContainsKey(RandomizedContext.RandomizedContextPropertyName))
                        return (RandomizedContext?)test.Properties.Get(RandomizedContext.RandomizedContextPropertyName);

                    return null;
                }

                public override TestResult Execute(TestExecutionContext context)
                {
                    TestResult result = context.CurrentResult;
                    Test currentTest = context.CurrentTest; // LUCENENET TODO: Make this more robust by checking whether it is the class
                    Test classTest = (Test)context.CurrentTest.Parent!;
                    RandomizedContext? randomizedContext = GetRandomizedContext(currentTest);  // context.CurrentTest.GetRandomizedContext();
                    if (randomizedContext is null)
                    {
                        // LUCENENET TODO: We should report an error here, since this attribute is not allowed outside of the test framework.
                        // For now, we are just skipping this attribute.
                        return innerCommand.Execute(context);
                    }

                    RandomizedContext? classRandomizedContext = GetRandomizedContext(classTest);
                    if (classRandomizedContext is null)
                    {
                        // LUCENENET TODO: We should report an error here, since this attribute is not allowed outside of the test framework.
                        // For now, we are just skipping this attribute.
                        return innerCommand.Execute(context);
                    }

                    //context.CurrentTest.Properties.Set(RandomizedContext.RandomizedContextPropertyName, classRandomizedContext);
                    context.CurrentTest = classTest;
                    try
                    {
                        var repeatRandom = new J2N.Randomizer(randomizedContext.RandomSeed);
                        // Undo whatever happened during the initial startup
                        var testFixtureInstance = context.TestObject;
                        if (testFixtureInstance is LuceneTestCase init)
                        {
                            init.OneTimeTearDown();
                        }


                        for (int i = 0; i < repeatCount; i++)
                        {
                            // Regenerate the random seeds for this iteration
                            //long randomSeed = repeatRandom.NextInt64();
                            long testSeed = repeatRandom.NextInt64();
                            //randomizedContext.ResetRandom(randomSeed, testSeed);
                            //var testRandom = new J2N.Randomizer(testSeed);
                            //var classRandom = new J2N.Randomizer(testSeed);
                            classRandomizedContext.ResetRandom(testSeed);
                            randomizedContext.ResetRandom(testSeed);


                            // Recreate test fixture for this iteration
                            //var testFixtureInstance = context.TestObject = Activator.CreateInstance(context.TestObject!.GetType());

                            if (testFixtureInstance is LuceneTestCase setup)
                            {
                                //randomizedContext.ResetRandom(classRandom);
                                try
                                {
                                    setup.OneTimeSetUp();
                                }
                                catch
                                {
                                    // LUCENENET TODO: Work out what to do with the error
                                }
                            }

                            //randomizedContext.ResetRandom(testRandom);
                            context.CurrentTest = currentTest;

                            //context.CurrentTest.Properties.Set("RunSeed", runSeed);
                            //context.RandomGenerator = new Random(runSeed); // Set a new random generator
                            result = innerCommand.Execute(context);

                            //randomizedContext.ResetRandom(classRandom);


                            //context.CurrentTest.Properties.Set(RandomizedContext.RandomizedContextPropertyName, classRandomizedContext);
                            context.CurrentTest = classTest;
                            if (testFixtureInstance is LuceneTestCase teardown)
                            {
                                try
                                {
                                    teardown.OneTimeTearDown();
                                }
                                catch
                                {
                                    // LUCENENET TODO: Work out what to do with the error
                                }
                            }
                            //context.CurrentTest.Properties.Set(RandomizedContext.RandomizedContextPropertyName, randomizedContext);
                            context.CurrentTest = currentTest;

                            if (result.ResultState == ResultState.Failure || result.ResultState == ResultState.Error)
                            {
                                string message = $"Repeat failed on iteration '{i}'.{Environment.NewLine}{Environment.NewLine}{result.Message}";
                                result.SetResult(result.ResultState, message, result.StackTrace);
                                break;
                            }
                        }
                    }
                    finally
                    {
                        //context.CurrentTest.Properties.Set(RandomizedContext.RandomizedContextPropertyName, randomizedContext);
                        context.CurrentTest = currentTest;
                    }

                    return result;
                }
            }
        }
    }
}
