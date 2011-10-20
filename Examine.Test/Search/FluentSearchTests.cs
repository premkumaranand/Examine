using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Examine.LuceneEngine.SearchCriteria;
using Examine.SearchCriteria;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Examine.Test.Search
{
    [TestClass]
    public class FluentSearchTests
    {
        [TestMethod]
        public void TestMethod1()
        {

            //get all data where name = shannon and age is between 18-100 and ( they live in either sydney or calgary )
            var qry = CreateQuery()
                .Must(x => x.Field("name", new ExamineValue(Examineness.Default, "shannon")))
                .Must(x => x.Range("age", 18, 100))
                .Group(BooleanOperation.And,
                       op => op.Should(x => x.Field("city", new ExamineValue(Examineness.Default, "calgary"))),
                       op => op.Should(x => x.Field("city", new ExamineValue(Examineness.Default, "sydney"))))
                .Compile()
                .OrderBy("id,name");

            //get the description that contains "hello" or "world" or "hello world" but boosting "hello" and "world" by 5 and boosting the
            //entire phrase by 10
            var qry2 = CreateQuery()
                .Must(x => x.Field("description",
                                   new ExamineValue(Examineness.Default,
                                                    "hello world".SplitPhrase(words => words.Boost(5), phrase => phrase.Boost(10)))))
                .Compile();

        }
    }
}
