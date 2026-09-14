using System.Collections.Generic;

namespace SmartX.API.Models
{
    /// <summary>
    /// Represents a physical or logical deployment node in a nested spatial hierarchy.
    /// Provides recursive validation across mesh sub-zones.
    /// </summary>
    public class DeploymentNode
    {
        public string NodeId { get; set; } = string.Empty;
        public string ZoneName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public bool IsSafetyCompliant { get; set; } = true;
        public List<DeploymentNode> SubZones { get; set; } = new List<DeploymentNode>();

        public DeploymentNode()
        {
        }

        public DeploymentNode(string nodeId, string zoneName, bool isActive = true, bool isSafetyCompliant = true)
        {
            NodeId = nodeId;
            ZoneName = zoneName;
            IsActive = isActive;
            IsSafetyCompliant = isSafetyCompliant;
        }

        /// <summary>
        /// Recursively validates node deployment safety across all nested child sub-zones.
        /// </summary>
        /// <param name="node">Root or current spatial deployment node.</param>
        /// <returns>True if the node and all descendant sub-zones are active and safety-compliant.</returns>
        public static bool ValidateHierarchyRecursively(DeploymentNode? node)
        {
            if (node == null)
            {
                return false;
            }

            if (!node.IsActive || !node.IsSafetyCompliant)
            {
                return false;
            }

            if (node.SubZones != null)
            {
                foreach (var child in node.SubZones)
                {
                    if (!ValidateHierarchyRecursively(child))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
