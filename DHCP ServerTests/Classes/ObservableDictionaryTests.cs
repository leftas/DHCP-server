using Microsoft.VisualStudio.TestTools.UnitTesting;
using DNS_Server.Classes;
using System;
using System.Collections.Generic;
using System.Text;

namespace DNS_Server.Classes.Tests
{
    [TestClass()]
    public class ObservableDictionaryTests
    {
        [TestMethod]
        public void RemovePredicate()
        {
            var observable = new ObservableDictionary<int, int>();

            for (int i = 0; i < 10; i++)
            {
                observable.Add(i, i);
            }

            observable.RemoveValues(x => x > 5);

            foreach (var a in observable)
            {
                Assert.IsTrue(a.Value <= 5);
            }
        }
    }
}