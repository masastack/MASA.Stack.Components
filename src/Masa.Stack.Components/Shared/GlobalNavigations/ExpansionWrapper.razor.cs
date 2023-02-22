﻿namespace Masa.Stack.Components.GlobalNavigations
{
    public partial class ExpansionWrapper : BDomComponentBase, IDisposable
    {
        [Inject]
        private IJSRuntime JsRuntime { get; set; } = null!;

        [Parameter, EditorRequired]
        public List<Category> Categories { get; set; } = new();

        [Parameter]
        public bool Checkable { get; set; }

        [Parameter]
        public bool InPreview { get; set; }

        [Parameter]
        public bool Favorite { get; set; }

        [Parameter]
        public string? Search { get; set; }

        [Parameter]
        public List<CategoryAppNav> Value
        {
            get => _value;
            set
            {
                _allValue = value;
                _value = value;
            }
        }

        [Parameter]
        public EventCallback<List<CategoryAppNav>> ValueChanged { get; set; }

        [Parameter]
        public string SideStyle { get; set; } = "";

        [Parameter]
        public string SideClass { get; set; } = "";

        [Parameter]
        public List<FavoriteNav> FavoriteNavs { get; set; } = new();

        public string TagIdPrefix { get; } = Guid.NewGuid().ToString();

        private DotNetObjectReference<ExpansionWrapper>? _objRef;
        private List<CategoryAppNav> _value = new();
        private List<CategoryAppNav> _allValue = new();

        IEnumerable<Category> FilterCategories()
        {
            return Categories.Where(categorie => categorie.Filter(Search));
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _objRef = DotNetObjectReference.Create(this);
                await JsRuntime.InvokeVoidAsync("MasaStackComponents.listenScroll", ".global-nav-content__main", ".category", _objRef);
            }

            await NextTickWhile(async () =>
            {
                await JsRuntime.InvokeVoidAsync("MasaStackComponents.setAppBorder");
            }, () => Categories == null || Categories.Any() is false);
        }

        internal async Task UpdateValues(string code, List<CategoryAppNav> value, CodeType type)
        {
            _allValue = _allValue.Where(v => v.App != code).ToList();
            _allValue.AddRange(value);
            await UpdateValue(_allValue);
        }

        private async Task ScrollTo(string tagId, string insideSelector)
        {
            await JsRuntime.InvokeVoidAsync("MasaStackComponents.scrollTo", $"#{tagId}", insideSelector);
        }

        private async Task UpdateValue(List<CategoryAppNav> value)
        {
            value = value.Distinct().ToList();

            if (ValueChanged.HasDelegate)
            {
                await ValueChanged.InvokeAsync(value);
            }
            else
            {
                Value = value;
            }
        }

        public new void Dispose()
        {
            _objRef?.Dispose();
            base.Dispose();
        }
    }
}
