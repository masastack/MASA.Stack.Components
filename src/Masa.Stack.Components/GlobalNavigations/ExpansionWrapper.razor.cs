using System.Text.Json;
using Microsoft.JSInterop;

namespace Masa.Stack.Components.GlobalNavigations
{
    public partial class ExpansionWrapper : IDisposable
    {
        [Inject]
        private IJSRuntime JsRuntime { get; set; } = null!;

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
        public List<CategoryAppNav>? Value { get; set; } = new();

        [Parameter]
        public EventCallback<List<CategoryAppNav>> ValueChanged { get; set; }

        [Parameter] 
        public string? TagIdPrefix { get; set; }

        private int _activeCategoryIndex;
        private bool _initValuesDic;
        private bool _fromCheckbox;
        private DotNetObjectReference<ExpansionWrapper>? _objRef;
        private Dictionary<string, List<CategoryAppNav>> _valuesDic = new();

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

            if (Checkable)
            {
                if (Value is not null && (!_initValuesDic || !_fromCheckbox))
                {
                    _initValuesDic = true;
                    _valuesDic.Clear();

                    foreach (var value in Value)
                    {
                        if (value.Category is not null && value.App is not null)
                        {
                            var categoryAppNav = new CategoryAppNav(value.Category, value.App, value.Nav);

                            var key = $"app_{value.App}";

                            if (_valuesDic.ContainsKey(key))
                            {
                                _valuesDic[key].Add(categoryAppNav);
                            }
                            else
                            {
                                _valuesDic.Add(key, new List<CategoryAppNav>() { categoryAppNav });
                            }
                        }
                        else if (value.Category is not null)
                        {
                            var categoryAppNav = new CategoryAppNav(value.Category);

                            var key = $"category_{value.Category}";

                            if (_valuesDic.ContainsKey(key))
                            {
                                _valuesDic[key].Add(categoryAppNav);
                            }
                            else
                            {
                                _valuesDic.Add(key, new List<CategoryAppNav>() { categoryAppNav });
                            }
                        }
                    }
                }

                if (_fromCheckbox)
                {
                    _fromCheckbox = false;
                }
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _objRef = DotNetObjectReference.Create(this);
                await JsRuntime.InvokeVoidAsync("MasaStackComponents.listenScroll", ".global-nav-content__main", ".category", _objRef);
            }
        }

        internal async Task UpdateValues(string key, List<CategoryAppNav> value)
        {
            _fromCheckbox = true;

            if (_valuesDic.TryGetValue(key, out _))
            {
                _valuesDic[key] = value;
            }
            else
            {
                _valuesDic.Add(key, value);
            }

            List<CategoryAppNav> values = new();
            _valuesDic.ForEach(item => { values.AddRange(item.Value); });

            await UpdateValue(values);

            StateHasChanged();
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

            Value = value;
        }

        public void Dispose()
        {
            _objRef?.Dispose();
        }
    }
}
