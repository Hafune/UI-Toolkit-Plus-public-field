using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace QuickEye.UIToolkit
{
    [UxmlElement]
    public partial class Tab : BaseBindable<bool>
    {
        public const string ClassName = "qe-tab";
        public const string TextClassName = ClassName + "__text";
        public const string CheckedClassName = ClassName + "--checked";
        public const string UncheckedClassName = ClassName + "--unchecked";

        public readonly Reorderable Reorderable = new(ClassName) { LockDragToAxis = true };

        private readonly Label _textElement;

        private VisualElement _tabContent;

        public Tab() : this(null) { }

        public Tab(string text)
        {
            this.InitResources();
            AddToClassList(ClassName);

            _textElement = new Label(text);
            _textElement.AddToClassList(TextClassName);
            Add(_textElement);

            RegisterCallback<PointerDownEvent>(PointerDownHandler);
            IsReorderable = false;
            SetActive(value);
        }

        public VisualElement TabContent
        {
            get => _tabContent;
            set
            {
                _tabContent = value;
                _tabContent?.ToggleDisplayStyle(this.value);
            }
        }

        [UxmlAttribute("is-reorderable")]
        public bool IsReorderable
        {
            get => Reorderable.target == this;
            set => this.ToggleManipulator(Reorderable, value);
        }

        [UxmlAttribute("text")]
        public string Text
        {
            get => _textElement.text;
            set => _textElement.text = value;
        }

        public bool IsDragged => Reorderable.IsDragged(this);

        [UxmlAttribute("value")]
        public bool UxmlValue
        {
            get => value;
            set => SetValueWithoutNotify(value);
        }

        protected virtual void PointerDownHandler(PointerDownEvent evt)
        {
            value = true;
        }

        public override void SetValueWithoutNotify(bool newValue)
        {
            base.SetValueWithoutNotify(newValue);
            SetActive(newValue);
        }

        private void SetActive(bool isActive)
        {
            EnableInClassList(CheckedClassName, isActive);
            EnableInClassList(UncheckedClassName, !isActive);
            TabContent?.ToggleDisplayStyle(isActive);
            if (isActive)
                DeactivateSiblings();
        }

        private void DeactivateSiblings()
        {
            if (parent == null)
                return;
            foreach (var tab in parent.Children().OfType<Tab>())
                if (tab != this)
                    tab.SetValueWithoutNotify(false);
        }

    }
}
