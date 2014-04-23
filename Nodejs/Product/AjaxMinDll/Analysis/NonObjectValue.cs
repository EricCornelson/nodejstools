﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.NodejsTools.Parsing;

namespace Microsoft.NodejsTools.Analysis {
    /// <summary>
    /// Represents a value which is not an object (number, string, bool)
    /// </summary>
    abstract class NonObjectValue : AnalysisValue {
        public abstract AnalysisValue Prototype {
            get;
        }

        public override Dictionary<string, IAnalysisSet> GetAllMembers() {
            return Prototype.GetAllMembers();
        }

        public override IAnalysisSet GetMember(Node node, AnalysisUnit unit, string name) {
            return Prototype.GetMember(node, unit, name);
        }
    }
}
