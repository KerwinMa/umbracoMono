﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Umbraco.Core.IO;
using Umbraco.Core.Models.Css;
using Umbraco.Core.Models.EntityBase;

namespace Umbraco.Core.Models
{
    /// <summary>
    /// Represents a Stylesheet file
    /// </summary>
    [Serializable]
    [DataContract(IsReference = true)]
    public class Stylesheet : File
    {
        public Stylesheet(string path) : base(path)
        {
            base.Path = path;
        }

        /// <summary>
        /// Returns a list of <see cref="StylesheetProperty"/>
        /// </summary>
        [IgnoreDataMember]
        public IEnumerable<StylesheetProperty> Properties
        {
            get
            {
                var properties = new List<StylesheetProperty>();
                var parser = new CssParser(Content);

                //TODO Need to explorer how the Stylesheet should be iterated to generate a list of css properties
                foreach (CssAtRule statement in parser.StyleSheet.Statements.Where(s => s is CssAtRule))
                {
                    properties.Add(new StylesheetProperty(statement.Value, ""));
                }

                foreach (CssRuleSet statement in parser.StyleSheet.Statements.Where(s => s is CssRuleSet))
                {
                    var selector = statement.Selectors.First();
                    var declaration = statement.Declarations.FirstOrDefault();
                    properties.Add(new StylesheetProperty(selector.Value, declaration.ToString()));
                }

                return properties;
            }
        }

        /// <summary>
        /// Boolean indicating whether the file could be validated
        /// </summary>
        /// <returns>True if file is valid, otherwise false</returns>
        public override bool IsValid()
        {
            var dirs = SystemDirectories.Css;

            //Validate file
            var validFile = IOHelper.ValidateEditPath(Path, dirs.Split(','));

            //Validate extension
            var validExtension = IOHelper.ValidateFileExtension(Path, new List<string> {"css"});

            return validFile && validExtension;
        }

        /// <summary>
        /// Boolean indicating whether the file is valid css using a css parser
        /// </summary>
        /// <returns>True if css is valid, otherwise false</returns>
        public bool IsFileValidCss()
        {
            var parser = new CssParser(Path);//TODO change CssParser so we can use Content instead of Path
            return parser.Errors.Any();
        }
    }

    public class StylesheetProperty : IValueObject
    {
        public StylesheetProperty(string @alias, string value)
        {
            Alias = alias;
            Value = value;
        }

        public string Alias { get; set; }
        public string Value { get; set; }
    }
}