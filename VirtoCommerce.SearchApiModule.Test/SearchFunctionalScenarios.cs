﻿using CacheManager.Core;
using System;
using VirtoCommerce.CatalogModule.Data.Repositories;
using VirtoCommerce.CatalogModule.Data.Services;
using VirtoCommerce.CoreModule.Data.Repositories;
using VirtoCommerce.CoreModule.Data.Services;
using VirtoCommerce.Domain.Catalog.Services;
using VirtoCommerce.Domain.Commerce.Services;
using VirtoCommerce.Domain.Pricing.Services;
using VirtoCommerce.Platform.Core.ChangeLog;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Platform.Data.ChangeLog;
using VirtoCommerce.Platform.Data.Infrastructure.Interceptors;
using VirtoCommerce.Platform.Data.Repositories;
using VirtoCommerce.PricingModule.Data.Repositories;
using VirtoCommerce.PricingModule.Data.Services;
using VirtoCommerce.SearchModule.Data.Services;
using Xunit;
using Xunit.Abstractions;
using System.Linq;
using System.Threading;
using VirtoCommerce.SearchModule.Data.Model.Search.Criterias;
using VirtoCommerce.SearchModule.Data.Model.Filters;
using VirtoCommerce.SearchModule.Data.Model.Indexing;
using VirtoCommerce.Domain.Store.Services;
using System.Collections.Generic;
using VirtoCommerce.StoreModule.Data.Services;
using VirtoCommerce.StoreModule.Data.Repositories;
using VirtoCommerce.Domain.Shipping.Services;
using VirtoCommerce.Domain.Shipping.Model;
using VirtoCommerce.Domain.Payment.Services;
using VirtoCommerce.Domain.Payment.Model;
using VirtoCommerce.Domain.Tax.Services;
using VirtoCommerce.Domain.Tax.Model;
using VirtoCommerce.Platform.Core.DynamicProperties;
using Moq;
using VirtoCommerce.Platform.Data.DynamicProperties;
using VirtoCommerce.SearchApiModule.Web.Services;
using VirtoCommerce.SearchApiModule.Web.Model;
using VirtoCommerce.Platform.Data.Assets;

namespace VirtoCommerce.SearchModule.Tests
{
    [CLSCompliant(false)]
    [Collection("Search")]
    public class SearchFunctionalScenarios : SearchTestsBase
    {
        private readonly ITestOutputHelper _output;

        public SearchFunctionalScenarios(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        //[InlineData("Lucene")]
        [InlineData("Elastic")]
        public void Can_index_category_demo_data_and_search_using_outline(string providerType)
        {
            var scope = "test";
            var provider = GetSearchProvider(providerType, scope);

            provider.RemoveAll(scope, "");
            var controller = GetSearchIndexController(provider);
            controller.Process(scope, "category", true);

            // sleep for index to be commited
            Thread.Sleep(5000);

            // find all prodducts in the category
            var categoryCriteria = new CategorySearchCriteria()
            {
            };

            categoryCriteria.Outlines.Add("4974648a41df4e6ea67ef2ad76d7bbd4/45d3fc9a913d4610a5c7d0470558*");


            var response = provider.Search<DocumentDictionary>(scope, categoryCriteria);
            Assert.True(response.TotalCount > 0, string.Format("Didn't find any categories using {0} search", providerType));
        }

        [Theory]
        [InlineData("Lucene")]
        [InlineData("Elastic")]
        public void Can_index_product_demo_data_and_search_using_outline(string providerType)
        {
            var scope = "test";
            var provider = GetSearchProvider(providerType, scope);

            provider.RemoveAll(scope, "");
            var controller = GetSearchIndexController(provider);
            controller.Process(scope, CatalogItemSearchCriteria.DocType, true);

            // sleep for index to be commited
            Thread.Sleep(5000);

            // get catalog id by name
            var catalogRepo = GetCatalogRepository();
            var catalog = catalogRepo.Catalogs.SingleOrDefault(x => x.Name.Equals("electronics", StringComparison.OrdinalIgnoreCase));

            // find all prodducts in the category
            var catalogCriteria = new CatalogItemSearchCriteria()
            {
                Catalog = catalog.Id,
                Currency = "USD"
            };

            catalogCriteria.Outlines.Add("4974648a41df4e6ea67ef2ad76d7bbd4/c76774f9047d4f18a916b38681c50557*");

            var ibs = GetItemBrowsingService(provider);
            var searchResults = ibs.SearchItems(scope, catalogCriteria, Domain.Catalog.Model.ItemResponseGroup.ItemLarge);

            Assert.True(searchResults.ProductsTotalCount > 0, string.Format("Didn't find any products using {0} search", providerType));
        }

        [Theory]
        [InlineData("Lucene")]
        [InlineData("Elastic")]
        public void Can_index_product_demo_data_and_search(string providerType)
        {
            var scope = "test";
            var provider = GetSearchProvider(providerType, scope);

            //if (provider is ElasticSearchProvider)
            //    (provider as ElasticSearchProvider).AutoCommitCount = 1; // commit every one document

            provider.RemoveAll(scope, "");
            var controller = GetSearchIndexController(provider);
            controller.Process(scope, CatalogItemSearchCriteria.DocType, true);

            // sleep for index to be commited
            Thread.Sleep(5000);

            // get catalog id by name
            var catalogRepo = GetCatalogRepository();
            var catalog = catalogRepo.Catalogs.SingleOrDefault(x => x.Name.Equals("electronics", StringComparison.OrdinalIgnoreCase));

            // find all prodducts in the category
            var catalogCriteria = new CatalogItemSearchCriteria()
            {
                Catalog = catalog.Id,
                Currency = "USD"
            };

            // Add all filters
            var brandFilter = new AttributeFilter { Key = "brand" };
            var filter = new AttributeFilter { Key = "color", IsLocalized = true };
            filter.Values = new[]
                                {
                                    new AttributeFilterValue { Id = "Red", Value = "Red" },
                                    new AttributeFilterValue { Id = "Gray", Value = "Gray" },
                                    new AttributeFilterValue { Id = "Black", Value = "Black" }
                                };

            var rangefilter = new RangeFilter { Key = "size" };
            rangefilter.Values = new[]
                                     {
                                         new RangeFilterValue { Id = "0_to_5", Lower = "0", Upper = "5" },
                                         new RangeFilterValue { Id = "5_to_10", Lower = "5", Upper = "10" }
                                     };

            var priceRangefilter = new PriceRangeFilter { Currency = "USD" };
            priceRangefilter.Values = new[]
                                          {
                                              new RangeFilterValue { Id = "under-100", Upper = "100" },
                                              new RangeFilterValue { Id = "200-600", Lower = "200", Upper = "600" }
                                          };

            catalogCriteria.Add(filter);
            catalogCriteria.Add(rangefilter);
            catalogCriteria.Add(priceRangefilter);
            catalogCriteria.Add(brandFilter);

            var ibs = GetItemBrowsingService(provider);
            var searchResults = ibs.SearchItems(scope, catalogCriteria, Domain.Catalog.Model.ItemResponseGroup.ItemLarge);

            Assert.True(searchResults.ProductsTotalCount > 0, string.Format("Didn't find any products using {0} search", providerType));
            Assert.True(searchResults.Aggregations.Count() > 0, string.Format("Didn't find any aggregations using {0} search", providerType));

            var colorAggregation = searchResults.Aggregations.SingleOrDefault(a => a.Field.Equals("color", StringComparison.OrdinalIgnoreCase));
            Assert.True(colorAggregation.Items.Where(x => x.Value.ToString().Equals("Red", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Count == 6);
            Assert.True(colorAggregation.Items.Where(x => x.Value.ToString().Equals("Gray", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Count == 3);
            Assert.True(colorAggregation.Items.Where(x => x.Value.ToString().Equals("Black", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Count == 13);

            var brandAggregation = searchResults.Aggregations.SingleOrDefault(a => a.Field.Equals("brand", StringComparison.OrdinalIgnoreCase));
            Assert.True(brandAggregation.Items.Where(x => x.Value.ToString().Equals("Beats By Dr Dre", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Count == 3);

            var keywordSearchCriteria = new KeywordSearchCriteria(CatalogItemSearchCriteria.DocType) { Currency = "USD", Locale = "en-us", SearchPhrase = "sony" };
            searchResults = ibs.SearchItems(scope, keywordSearchCriteria, Domain.Catalog.Model.ItemResponseGroup.ItemLarge);
            Assert.True(searchResults.ProductsTotalCount > 0);
        }

        [Theory]
        [InlineData("Lucene")]
        [InlineData("Elastic")]
        public void Can_web_search_products(string providerType)
        {
            var scope = "test";
            var storeName = "electronics";
            var provider = GetSearchProvider(providerType, scope);

            provider.RemoveAll(scope, "");
            var controller = GetSearchIndexController(provider);
            controller.Process(scope, CatalogItemSearchCriteria.DocType, true);

            // sleep for index to be commited
            Thread.Sleep(5000);

            // get catalog id by name
            var catalogRepo = GetCatalogRepository();
            var catalog = catalogRepo.Catalogs.SingleOrDefault(x => x.Name.Equals("electronics", StringComparison.OrdinalIgnoreCase));

            var storeRepo = GetStoreRepository();
            var store = storeRepo.Stores.SingleOrDefault(x => x.Name.Equals(storeName, StringComparison.OrdinalIgnoreCase));

            // find all prodducts in the category
            var criteria = new ProductSearch()
            {
                Catalog = catalog.Id,
                Currency = "USD",
                Facets = new[] { "brand", "size", "price:0_to_5", "price:5_to_10" }
            };


            var context = new Dictionary<string, object>
            {
                { "StoreId", store.Id },
            };

            var filterService = GetBrowseFilterService();
            var filters = filterService.GetFilters(context);
            var serviceCriteria = criteria.AsCriteria<CatalogItemSearchCriteria>(filters);
            var ibs = GetItemBrowsingService(provider);

            //Load ALL products 
            var searchResults = ibs.SearchItems(scope, serviceCriteria, Domain.Catalog.Model.ItemResponseGroup.ItemLarge);

            Assert.True(searchResults.ProductsTotalCount > 0, string.Format("Didn't find any products using {0} search", providerType));
            Assert.True(searchResults.Aggregations.Count() > 0, string.Format("Didn't find any aggregations using {0} search", providerType));

            var colorAggregation = searchResults.Aggregations.SingleOrDefault(a => a.Field.Equals("color", StringComparison.OrdinalIgnoreCase));
            Assert.True(colorAggregation.Items.Where(x => x.Value.ToString().Equals("Red", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Count == 6);
            Assert.True(colorAggregation.Items.Where(x => x.Value.ToString().Equals("Gray", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Count == 3);
            Assert.True(colorAggregation.Items.Where(x => x.Value.ToString().Equals("Black", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Count == 13);

            var brandAggregation = searchResults.Aggregations.SingleOrDefault(a => a.Field.Equals("brand", StringComparison.OrdinalIgnoreCase));
            Assert.True(brandAggregation.Items.Where(x => x.Value.ToString().Equals("Beats By Dr Dre", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Count == 3);

            // now test sorting
            criteria = new ProductSearch()
            {
                Catalog = catalog.Id,
                Currency = "USD",
                Sort = new [] { "name" }
            };

            serviceCriteria = criteria.AsCriteria<CatalogItemSearchCriteria>(filters);
            searchResults = ibs.SearchItems(scope, serviceCriteria, Domain.Catalog.Model.ItemResponseGroup.ItemLarge);

            var productName = searchResults.Products[0].Name;
            Assert.True(productName == "2 Pack White Gem Burst Earcuffs");

            criteria = new ProductSearch()
            {
                Catalog = catalog.Id,
                Currency = "USD",
                Sort = new[] { "name desc" }
            };

            serviceCriteria = criteria.AsCriteria<CatalogItemSearchCriteria>(filters);
            searchResults = ibs.SearchItems(scope, serviceCriteria, Domain.Catalog.Model.ItemResponseGroup.ItemLarge);

            productName = searchResults.Products[0].Name;

            Assert.True(productName == "xFold CINEMA X12 RTF U7");
        }

        private ItemBrowsingService GetItemBrowsingService(Data.Model.ISearchProvider provider)
        {
            var service = new ItemBrowsingService(GetItemService(), provider, new FileSystemBlobProvider("", "http://samplesite.com"));
            return service;
        }

        private SearchIndexController GetSearchIndexController(Data.Model.ISearchProvider provider)
        {
            var settings = new Moq.Mock<ISettingsManager>();
            return new SearchIndexController(settings.Object, provider,
                new CatalogItemIndexBuilder(provider, GetSearchService(), GetItemService(), GetPricingService(), GetChangeLogService()),
                new CategoryIndexBuilder(provider, GetSearchService(), GetCategoryService(), GetChangeLogService()));
        }

        private ICommerceService GetCommerceService()
        {
            return new CommerceServiceImpl(GetCommerceRepository);
        }

        private ICatalogSearchService GetSearchService()
        {
            return new CatalogSearchServiceImpl(GetCatalogRepository, GetItemService(), GetCatalogService(), GetCategoryService());
        }

        private IOutlineService GetOutlineService()
        {
            return new OutlineService(GetCatalogRepository);
        }

        private IPricingService GetPricingService()
        {
            var cacheManager = new Moq.Mock<ICacheManager<object>>();
            return new PricingServiceImpl(GetPricingRepository, GetItemService(), null, cacheManager.Object, null, null, null);
        }

        private Data.Model.Filters.IBrowseFilterService GetBrowseFilterService()
        {
            return new FilterService(GetStoreService());
        }

        private IStoreService GetStoreService()
        {
            var cacheManager = new Moq.Mock<ICacheManager<object>>();
            var shippingService = Moq.Mock.Of<IShippingMethodsService>(s => s.GetAllShippingMethods() == new ShippingMethod[] { });
            var paymentService = Moq.Mock.Of<IPaymentMethodsService>(s => s.GetAllPaymentMethods() == new PaymentMethod[] { });
            var taxService = Moq.Mock.Of<ITaxService>(s => s.GetAllTaxProviders() == new TaxProvider[] { });
            var settings = Moq.Mock.Of<ISettingsManager>(s => s.GetModuleSettings("VirtoCommerce.Store") == new SettingEntry[] { });
            var dpService = GetDynamicPropertyService();

            return new StoreServiceImpl(GetStoreRepository, GetCommerceService(), settings, dpService, shippingService, paymentService, taxService, cacheManager.Object);
        }

        private IDynamicPropertyService GetDynamicPropertyService()
        {
            var service = new DynamicPropertyService(() => GetPlatformRepository());
            return service;
        }

        private IPropertyService GetPropertyService()
        {
            return new PropertyServiceImpl(GetCatalogRepository);
        }

        private ICategoryService GetCategoryService()
        {
            return new CategoryServiceImpl(GetCatalogRepository, GetCommerceService(), GetOutlineService());
        }

        private ICatalogService GetCatalogService()
        {
            return new CatalogServiceImpl(GetCatalogRepository, GetCommerceService());
        }

        private IItemService GetItemService()
        {
            return new ItemServiceImpl(GetCatalogRepository, GetCommerceService(), GetOutlineService());
        }

        private IChangeLogService GetChangeLogService()
        {
            return new ChangeLogService(GetPlatformRepository);
        }

        private IStoreRepository GetStoreRepository()
        {
            var result = new StoreRepositoryImpl("VirtoCommerce", new EntityPrimaryKeyGeneratorInterceptor(), new AuditableInterceptor(null));
            return result;
        }

        private IPlatformRepository GetPlatformRepository()
        {
            var result = new PlatformRepository("VirtoCommerce", new EntityPrimaryKeyGeneratorInterceptor(), new AuditableInterceptor(null));
            return result;
        }

        private IPricingRepository GetPricingRepository()
        {
            var result = new PricingRepositoryImpl("VirtoCommerce", new EntityPrimaryKeyGeneratorInterceptor(), new AuditableInterceptor(null));
            return result;
        }

        private ICatalogRepository GetCatalogRepository()
        {
            var result = new CatalogRepositoryImpl("VirtoCommerce", new EntityPrimaryKeyGeneratorInterceptor(), new AuditableInterceptor(null));
            return result;
        }

        private static IСommerceRepository GetCommerceRepository()
        {
            var result = new CommerceRepositoryImpl("VirtoCommerce", new EntityPrimaryKeyGeneratorInterceptor(), new AuditableInterceptor(null));
            return result;
        }
    }
}