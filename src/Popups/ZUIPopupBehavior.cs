using UnityEngine;

namespace ZUI.Popups
{
    /// <summary>
    /// Component attached to spawned ZUI popups to hold their configuration options
    /// and track ESC key dismissal behavior.
    /// </summary>
    public class ZUIPopupBehavior : MonoBehaviour
    {
        public PopupOptions Options { get; set; }
    }
}
