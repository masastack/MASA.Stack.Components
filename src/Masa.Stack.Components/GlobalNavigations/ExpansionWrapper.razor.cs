namespace Masa.Stack.Components.GlobalNavigations
{
    public partial class ExpansionWrapper : IDisposable
    {
        [Inject]
        private IJSRuntime JsRuntime { get; set; } = null!;

        [Parameter]
        public string Style { get; set; } = "";

        [Parameter]
        public string Class { get; set; } = "";

        [Parameter, EditorRequired]
        public List<Category>? Categories { get; set; }

        [Parameter]
        public bool Checkable { get; set; }

        [Parameter]
        public bool CheckStrictly { get; set; }

        [Parameter]
        public bool InPreview { get; set; }

        [Parameter]
        public List<FavoriteNav>? FavoriteNavs { get; set; }

        [Parameter]
        public List<CategoryAppNav> Value { get; set; } = new();

        [Parameter]
        public EventCallback<List<CategoryAppNav>> ValueChanged { get; set; }

        [Parameter]
        public string? TagIdPrefix { get; set; }

        private int _activeCategoryIndex;
        private DotNetObjectReference<ExpansionWrapper>? _objRef;
        private List<CategoryAppNav>? _allValue;

        private Dictionary<string, List<StringNumber>> CategoryCodes
        {
            get
            {
                var codes = new Dictionary<string, List<StringNumber>>();

                Categories!.ForEach(category => { codes.Add(category.Code, category.Apps.Select(app => (StringNumber)app.Code).ToList()); });

                return codes;
            }
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            Categories ??= new();
            FavoriteNavs ??= new();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _objRef = DotNetObjectReference.Create(this);
                await JsRuntime.InvokeVoidAsync("MasaStackComponents.listenScroll", ".global-nav-content__main", ".category", _objRef);
            }
        }

        internal async Task UpdateValues(string code, List<CategoryAppNav> value, CodeType type)
        {
            if (_allValue is null) _allValue = Value;
            _allValue = _allValue.Where(v => v.App != code).ToList();
            _allValue.AddRange(value);
            await UpdateValue(_allValue);
        }

        private async Task ScrollTo(string tagId, string insideSelector)
        {
            await JsRuntime.InvokeVoidAsync("MasaStackComponents.scrollTo", $"#{tagId}", insideSelector);
        }

        [JSInvokable]
        public void ComputeActiveCategory(int activeIndex)
        {
            _activeCategoryIndex = activeIndex;
            StateHasChanged();
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

        public void Dispose()
        {
            _objRef?.Dispose();
        }
    }
}
