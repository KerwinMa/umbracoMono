﻿using System;
using System.Collections.Generic;
using NUnit.Framework;
using Umbraco.Core;
using Umbraco.Core.PropertyEditors;
using Umbraco.Tests.TestHelpers;
using System.Threading;
using System.Globalization;

namespace Umbraco.Tests.PropertyEditors
{
	[TestFixture]
	public class PropertyEditorValueConverterTests
	{
		[Test]
		public void CanConvertDatePickerPropertyEditor()
		{
			Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-GB");

			var converter = new DatePickerPropertyEditorValueConverter();
			var sourceDateTime = new DateTime(2012, 11, 10, 13, 14, 15);

			var testCases = new Dictionary<string, bool>
			{
				{"2012-11-10", true},
				{"2012/11/10", true},
				{"10/11/2012", true},
				{"11/10/2012", false},
				{"Sat 10, Nov 2012", true},
				{"Saturday 10, Nov 2012", true},
				{"Sat 10, November 2012", true},
				{"Saturday 10, November 2012", true},
				{"2012-11-10 13:14:15", true},
				{"", false}
			};

			foreach (var testCase in testCases)
			{
				var result = converter.ConvertPropertyValue(testCase.Key);

				Assert.IsTrue(result.Success);

				var resultDate = ((DateTime)result.Result).Date;
				Assert.AreEqual(DateTime.Equals(sourceDateTime.Date, resultDate), testCase.Value);
			}
		}

		[Test]
		public void CanConvertYesNoPropertyEditor()
		{
			var converter = new YesNoPropertyEditorValueConverter();
			var testCases = new Dictionary<string, bool>
			{
				{"TRUE", true},
				{"True", true},
				{"true", true},
				{"1", true},
				{"FALSE", false},
				{"False", false},
				{"false", false},
				{"0", false},
				{"", false}
			};

			foreach (var testCase in testCases)
			{
				var result = converter.ConvertPropertyValue(testCase.Key);

				Assert.IsTrue(result.Success);
				Assert.AreEqual(testCase.Value, result.Result);
			}
		}
	}
}
