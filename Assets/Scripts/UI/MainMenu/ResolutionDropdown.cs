using TMPro;

namespace Pulsar.UI
{
    public class ResolutionDropdown : TMP_Dropdown
    {
        private bool _wasExpanded;

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(IsExpanded ? SelectionState.Normal : state, instant);
        }

        private void LateUpdate()
        {
            if (_wasExpanded == IsExpanded) return;
            _wasExpanded = IsExpanded;
            DoStateTransition(currentSelectionState, false);
        }
    }
}
