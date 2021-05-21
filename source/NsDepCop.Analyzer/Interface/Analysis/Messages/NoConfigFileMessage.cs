﻿namespace Codartis.NsDepCop.Core.Interface.Analysis.Messages
{
    /// <summary>
    /// A message indicating that no config file was found for a project or location.
    /// </summary>
    public sealed class NoConfigFileMessage : IssueMessageBase
    {
        public override IssueDescriptor IssueDefinition => IssueDefinitions.NoConfigFileIssue;
    }
}