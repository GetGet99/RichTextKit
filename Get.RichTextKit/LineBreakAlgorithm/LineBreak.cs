﻿// This file has been edited and modified from its original version.
// The original version of this file can be found at https://github.com/toptensoftware/RichTextKit/.
// Original copyright notice is below.
// RichTextKit
// Copyright © 2019-2020 Topten Software. All Rights Reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may 
// not use this product except in compliance with the License. You may obtain 
// a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT 
// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
// License for the specific language governing permissions and limitations 
// under the License.
//
// Ported from: https://github.com/foliojs/linebreak

using System.Diagnostics;

namespace Get.RichTextKit
{
    /// <summary>
    /// Information about a potential line break position
    /// </summary>
    [DebuggerDisplay("{PositionMeasure}/{PositionWrap} @ {Required}")]
    internal struct LineBreak
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="positionMeasure">The code point index to measure to</param>
        /// <param name="positionWrap">The code point index to actually break the line at</param>
        /// <param name="required">True if this is a required line break; otherwise false</param>
        public LineBreak(int positionMeasure, int positionWrap, bool required = false)
        {
            this.PositionMeasure = positionMeasure;
            this.PositionWrap = positionWrap;
            this.Required = required;
        }


        /// <summary>
        /// The break position, before any trailing whitespace
        /// </summary>
        /// <remarks>
        /// This doesn't include trailing whitespace
        /// </remarks>
        public int PositionMeasure;

        /// <summary>
        /// The break position, after any trailing whitespace
        /// </summary>
        /// <remarks>
        /// This includes trailing whitespace
        /// </remarks>
        public int PositionWrap;

        /// <summary>
        /// True if there should be a forced line break here
        /// </summary>
        public bool Required;
    }
}
