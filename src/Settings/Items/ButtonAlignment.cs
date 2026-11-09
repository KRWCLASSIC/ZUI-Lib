namespace ZUI.Settings
{
    /// <summary>
    /// Layout alignment for buttons within a settings row.
    /// </summary>
    public enum ButtonAlignment
    {
        /// <summary>
        /// Centered (default): Label and button are grouped and horizontally centered together, matching native checkboxes.
        /// </summary>
        Center,

        /// <summary>
        /// Right-aligned: Label and button are grouped and aligned towards the right side of the row.
        /// </summary>
        Right,

        /// <summary>
        /// Left-aligned: Label and button are grouped and aligned towards the left side of the row.
        /// </summary>
        Left,

        /// <summary>
        /// Fill / Full-width: Button stretches across the full available row width.
        /// </summary>
        Fill
    }
}
