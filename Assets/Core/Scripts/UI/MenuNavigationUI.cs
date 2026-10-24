using UnityEngine;
using UnityEngine.UI;

namespace Gavi
{
    /// <summary>The window "Menu navigation": a main switch and one tick box per way of moving through the menu (see <see cref="MenuNavigation"/>).</summary>
    public class MenuNavigationUI : MonoBehaviour
    {
        [SerializeField] private Toggle _toggleMaster;
        [Tooltip("One tick box per option, in the order of MenuNavigation.Option.")]
        [SerializeField] private Toggle[] _toggles;
        [Tooltip("Optional: sets everything back to the defaults.")]
        [SerializeField] private Button _buttonReset;

        private bool _syncing;

        private void Awake()
        {
            if (_toggleMaster != null)
                _toggleMaster.onValueChanged.AddListener(on => { if (!_syncing) MenuNavigation.Master = on; Refresh(); });
            for (int i = 0; i < _toggles.Length; i++)
            {
                int index = i;
                if (_toggles[i] != null)
                    _toggles[i].onValueChanged.AddListener(on => { if (!_syncing) MenuNavigation.Set((MenuNavigation.Option)index, on); });
            }
            if (_buttonReset != null)
                _buttonReset.onClick.AddListener(() => { MenuNavigation.ResetToDefaults(); Refresh(); });
        }

        private void OnEnable() => Refresh();

        // the single switches are dimmed while the main switch is off
        private void Refresh()
        {
            _syncing = true;
            if (_toggleMaster != null)
                _toggleMaster.SetIsOnWithoutNotify(MenuNavigation.Master);
            for (int i = 0; i < _toggles.Length; i++)
            {
                if (_toggles[i] == null)
                    continue;
                _toggles[i].SetIsOnWithoutNotify(MenuNavigation.Get((MenuNavigation.Option)i));
                _toggles[i].interactable = MenuNavigation.Master;
            }
            _syncing = false;
        }
    }
}
