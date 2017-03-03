﻿namespace TestRunner
{
    using NUnit.Framework.Interfaces;

    class CompositeListener : ITestListener
    {
        public CompositeListener(params ITestListener[] listeners)
        {
            testListeners = listeners;
        }

        public void TestStarted(ITest test)
        {
            foreach (var testListener in testListeners)
            {
                testListener.TestStarted(test);
            }
        }

        public void TestFinished(ITestResult result)
        {
            foreach (var testListener in testListeners)
            {
                testListener.TestFinished(result);
            }
        }

        public void TestOutput(TestOutput output)
        {
            foreach (var testListener in testListeners)
            {
                testListener.TestOutput(output);
            }
        }

        private ITestListener[] testListeners;
    }
}