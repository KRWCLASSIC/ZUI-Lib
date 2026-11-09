using System;

namespace ZUI.MainMenu
{
    /// <summary>
    /// Represents a mod button registered to appear in the Main Menu Tabbed Mode.
    /// </summary>
    public class MainMenuButtonRegistration
    {
        public string Id { get; set; }
        public string Label { get; set; }
        public Action OnClick { get; set; }
        public int Priority { get; set; } = 0;
        public bool Interactable { get; set; } = true;
        public Func<bool> VisibilityCondition { get; set; }

        public MainMenuButtonRegistration(string id, string label, Action onClick, int priority = 0)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Label = label ?? throw new ArgumentNullException(nameof(label));
            OnClick = onClick ?? throw new ArgumentNullException(nameof(onClick));
            Priority = priority;
        }

        public bool IsVisible => VisibilityCondition == null || VisibilityCondition();
    }
}
