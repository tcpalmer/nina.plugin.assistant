﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Assistant.NINAPlugin.Database.Migrate {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class SQL {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal SQL() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Assistant.NINAPlugin.Database.Migrate.SQL", typeof(SQL).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to /*
        ///*/
        ///
        ///ALTER TABLE project DROP COLUMN startdate;
        ///ALTER TABLE project DROP COLUMN enddate;
        ///
        ///PRAGMA user_version = 1;
        ///.
        /// </summary>
        internal static string _1 {
            get {
                return ResourceManager.GetString("1", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to /*
        ///*/
        ///
        ///CREATE TABLE IF NOT EXISTS `profilepreference` (
        ///	`Id`			INTEGER NOT NULL,
        ///	`profileId`		TEXT NOT NULL,
        ///	`enableGradeRMS`	INTEGER,
        ///	`enableGradeStars`	INTEGER,
        ///	`enableGradeHFR`	INTEGER,
        ///	`maxGradingSampleSize`		INTEGER,
        ///	`rmsPixelThreshold`			REAL,
        ///	`detectedStarsSigmaFactor`	REAL,
        ///	`hfrSigmaFactor`			REAL,
        ///	PRIMARY KEY(`id`)
        ///);
        ///
        ///PRAGMA user_version = 2;
        ///.
        /// </summary>
        internal static string _2 {
            get {
                return ResourceManager.GetString("2", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to /*
        ///*/
        ///
        ///ALTER TABLE acquiredimage ADD COLUMN rejectreason TEXT;
        ///
        ///PRAGMA user_version = 3;
        ///.
        /// </summary>
        internal static string _3 {
            get {
                return ResourceManager.GetString("3", resourceCulture);
            }
        }
    }
}
