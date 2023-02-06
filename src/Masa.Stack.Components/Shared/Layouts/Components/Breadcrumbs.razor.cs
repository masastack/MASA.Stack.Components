namespace Masa.Stack.Components.Layouts
{
    public partial class Breadcrumbs : MasaComponentBase
    {
        [Parameter, EditorRequired]
        public List<Nav> FlattenedNavs { get; set; } = new();

        protected override void OnParametersSet()
        {
            var url = new Uri(NavigationManager.Uri).AbsolutePath;

            var currentNav = FlattenedNavs.FirstOrDefault(n => n.Url == url);
            if (currentNav == null)
            {
                Items = new List<BreadcrumbItem>();
                return;
            }

            var parents = GetParents(currentNav.ParentCode);
            parents.Add(currentNav);

            Items = parents.Select(n => new BreadcrumbItem()
            {
                Exact = true,
                Href = n.Url,             
                Text = n.Name
            }).ToList();
        }

        private List<BreadcrumbItem> Items { get; set; } = new();

        private IList<Nav> GetParents(string parentCode)
        {
            var parents = new List<Nav>();

            var found = FlattenedNavs.FirstOrDefault(nav => nav.Code == parentCode);

            if (found is not null)
            {
                if (!string.IsNullOrEmpty(found.ParentCode))
                {
                    parents.AddRange(GetParents(found.ParentCode));
                }

                parents.Add(found);
            }

            return parents;
        }
    }
}
