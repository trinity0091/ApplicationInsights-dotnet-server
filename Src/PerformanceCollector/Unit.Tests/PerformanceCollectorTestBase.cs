﻿namespace Unit.Tests
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.StandardPerformanceCollector;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.WebAppPerformanceCollector;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// PerformanceCollector test base.
    /// </summary>
    public class PerformanceCollectorTestBase
    { 
        internal void PerformanceCollectorSanityTest(IPerformanceCollector collector, string counter, string categoryName, string counterName, string instanceName)
        {
            const int CounterCount = 3;

            for (int i = 0; i < CounterCount; i++)
            {
                string error;
                collector.RegisterCounter(
                    counter,
                    null,
                    true,
                    out error,
                    false);
            }

            var results = collector.Collect().ToList();

            Assert.AreEqual(CounterCount, results.Count);

            foreach (var result in results)
            {
                var value = result.Item2;

                Assert.AreEqual(categoryName,  result.Item1.CategoryName);
                Assert.AreEqual(counterName,  result.Item1.CounterName);

                if (instanceName != null)
                {
                    Assert.AreEqual(instanceName,  result.Item1.InstanceName);
                }

                Assert.IsTrue(value >= 0 && value <= 100);
            }
        }

        internal void PerformanceCollectorRefreshCountersTest(IPerformanceCollector collector)
        {
            var counters = new PerformanceCounter[]
                               {
                                   new PerformanceCounter("Processor", "% Processor Time", "_Total123blabla"),
                                   new PerformanceCounter("Processor", "% Processor Time", "_Total"),
                                   new PerformanceCounter("Processor", "% Processor Time", "_Total123afadfdsdf"), 
                               };

            foreach (var pc in counters)
            {
                try
                {
                    string error = null;
                    collector.RegisterCounter(
                        PerformanceCounterUtility.FormatPerformanceCounter(pc), 
                        null,
                        true,
                        out error,
                        false);
                }
                catch (Exception)
                {
                }
            }

            collector.RefreshCounters();
            
            // All bad state counters are removed and added later through register counter, and as a result, the order of the performance coutners is changed.
            Assert.AreEqual(collector.PerformanceCounters.First().InstanceName, "_Total");
            Assert.AreEqual(collector.PerformanceCounters.Last().InstanceName, "_Total123afadfdsdf");
        }

        internal void PerformanceCollectorBadStateTest(IPerformanceCollector collector)
        {
            var counters = new PerformanceCounter[]
                               {
                                   new PerformanceCounter("Processor", "% Processor Time", "_Total123blabla"),
                                   new PerformanceCounter("Processor", "% Processor Time", "_Total") 
                               };

            foreach (var pc in counters)
            {
                try
                {
                    string error = null;
                    collector.RegisterCounter(
                        PerformanceCounterUtility.FormatPerformanceCounter(pc), 
                        null,
                        true,
                        out error,
                        false);
                }
                catch (Exception)
                {
                }
            }

            Assert.IsTrue(collector.PerformanceCounters.First().IsInBadState);
            Assert.IsFalse(collector.PerformanceCounters.Last().IsInBadState);
        }

        internal void PerformanceCollectorAddRemoveCountersTest(StandardPerformanceCollector collector)
        {
            var counters = new[]
                               {
                                   new PerformanceCounter("Processor", "% Processor Time", "_Total"),
                                   new PerformanceCounter("Memory", "Available Bytes", "")
                               };

            foreach (var pc in counters)
            {
                string error;
                collector.RegisterCounter(PerformanceCounterUtility.FormatPerformanceCounter(pc), pc.GetHashCode().ToString(), true, out error, false);
            }

            var twoCounters = collector.PerformanceCounters.ToArray();

            collector.RemoveCounter(@"\PROCESSOR(_Total)\% Processor Time", counters[0].GetHashCode().ToString());

            var oneCounter = collector.PerformanceCounters.ToArray();

            Assert.AreEqual(2, twoCounters.Count());
            Assert.AreEqual(@"\Processor(_Total)\% Processor Time", twoCounters[0].OriginalString);
            Assert.AreEqual(@"\Memory\Available Bytes", twoCounters[1].OriginalString);

            Assert.AreEqual(@"\Memory\Available Bytes", oneCounter.Single().OriginalString);
        }

        internal void PerformanceCollectorAddRemoveCountersForWebAppTest(WebAppPerformanceCollector collector)
        {
            var counters = new[]
                               {
                                   new PerformanceCounter("ASP.NET Applications", "Request Execution Time", "??APP_W3SVC_PROC??"),
                                   new PerformanceCounter("ASP.NET Applications", "Requests In Application Queue", "??APP_W3SVC_PROC??")
                               };

            foreach (var pc in counters)
            {
                string error;
                collector.RegisterCounter(PerformanceCounterUtility.FormatPerformanceCounter(pc), pc.GetHashCode().ToString(), true, out error, false);
            }

            var twoCounters = collector.PerformanceCounters.ToArray();

            collector.RemoveCounter(@"\ASP.NET APPLICATIONS(??APP_W3SVC_PROC??)\Request Execution Time", counters[0].GetHashCode().ToString());

            var oneCounter = collector.PerformanceCounters.ToArray();

            Assert.AreEqual(2, twoCounters.Count());
            Assert.AreEqual(@"\ASP.NET Applications(??APP_W3SVC_PROC??)\Request Execution Time", twoCounters[0].OriginalString);
            Assert.AreEqual(@"\ASP.NET Applications(??APP_W3SVC_PROC??)\Requests In Application Queue", twoCounters[1].OriginalString);

            Assert.AreEqual(@"\ASP.NET Applications(??APP_W3SVC_PROC??)\Requests In Application Queue", oneCounter.Single().OriginalString);
        }
    }
}