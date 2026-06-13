namespace SolutionDeploymentAdvisor.Models
{
    /// <summary>
    /// The user's choice for a patch group that already has an open patch in source.
    /// </summary>
    public enum PatchDecision
    {
        CreateNew,
        AppendToExisting
    }

    /// <summary>
    /// Represents one row in the PatchDecisionDialog — one row per patch-group
    /// (base-solution) that will be touched during the Create operation.
    /// </summary>
    public class PatchDecisionRow
    {
        /// <summary>Base solution unique name (the patch parent key).</summary>
        public string BaseSolution { get; set; } = string.Empty;

        /// <summary>
        /// The existing open patch found in the source environment, or <c>null</c>
        /// when no open patch exists for this base solution.
        /// </summary>
        public SolutionItem? ExistingPatch { get; set; }

        /// <summary>
        /// The new patch version that would be created if the user picks
        /// <see cref="PatchDecision.CreateNew"/>.
        /// This is guaranteed to be strictly greater than both
        /// <see cref="TargetHighestVersion"/> and <see cref="SourceHighestVersion"/>.
        /// </summary>
        public string ProposedNewVersion { get; set; } = string.Empty;

        /// <summary>Highest version already present in the TARGET environment for this base solution.</summary>
        public string TargetHighestVersion { get; set; } = string.Empty;

        /// <summary>Highest version already present in the SOURCE environment for this base solution.</summary>
        public string SourceHighestVersion { get; set; } = string.Empty;

        /// <summary>
        /// The user's final decision for this row, written back by
        /// <see cref="UI.PatchDecisionDialog"/> when the user confirms.
        /// </summary>
        public PatchDecision Decision { get; set; } = PatchDecision.CreateNew;

        /// <summary>Back-reference to the <see cref="SolutionPreview"/> this row controls.</summary>
        public SolutionPreview Preview { get; set; } = null!;
    }
}
