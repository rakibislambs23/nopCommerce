﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Nop.Core;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Plugins;

namespace Nop.Plugin.DiscountRules.AddressRole
{
    public partial class CustomerRoleDiscountRequirementRule : BasePlugin, IDiscountRequirementRule
    {
        #region Fields

        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly ICustomerService _customerService;
        private readonly IDiscountService _discountService;
        private readonly ILocalizationService _localizationService;
        private readonly ISettingService _settingService;
        private readonly IUrlHelperFactory _urlHelperFactory;
        private readonly IWebHelper _webHelper;
        private readonly IOrderService _orderService;

        #endregion

        #region Ctor

        public CustomerRoleDiscountRequirementRule(IActionContextAccessor actionContextAccessor,
            IDiscountService discountService,
            ICustomerService customerService,
            ILocalizationService localizationService,
            ISettingService settingService,
            IUrlHelperFactory urlHelperFactory,
            IWebHelper webHelper,
            IOrderService orderService)
        {
            _actionContextAccessor = actionContextAccessor;
            _customerService = customerService;
            _discountService = discountService;
            _localizationService = localizationService;
            _settingService = settingService;
            _urlHelperFactory = urlHelperFactory;
            _webHelper = webHelper;
            _orderService = orderService;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Check discount requirement
        /// </summary>
        /// <param name="request">Object that contains all information required to check the requirement (Current customer, discount, etc)</param>
        /// <returns>Result</returns>
        public DiscountRequirementValidationResult CheckRequirement(DiscountRequirementValidationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            //invalid by default
            var result = new DiscountRequirementValidationResult();

            if (request.Customer == null)
                return result;
            
            //check if rule is applied
            var isAddressRuleApplied = _settingService.GetSettingByKey<bool>(string.Format(DiscountRequirementDefaults.SettingsKey, request.DiscountRequirementId));
            if (isAddressRuleApplied == false)
                return result;

            //check customer orders
            var orders = _orderService.SearchOrders(storeId: request.Store.Id, customerId: request.Customer.Id);
            result.IsValid = orders.Count == 0 ? true : false;
            return result;
        }


        /// <summary>
        /// Get URL for rule configuration
        /// </summary>
        /// <param name="discountId">Discount identifier</param>
        /// <param name="discountRequirementId">Discount requirement identifier (if editing)</param>
        /// <returns>URL</returns>
        public string GetConfigurationUrl(int discountId, int? discountRequirementId)
        {
            var urlHelper = _urlHelperFactory.GetUrlHelper(_actionContextAccessor.ActionContext);

            return urlHelper.Action("Configure", "DiscountRulesAddressRole",
                new { discountId = discountId, discountRequirementId = discountRequirementId }, _webHelper.CurrentRequestProtocol);
        }

        /// <summary>
        /// Install the plugin
        /// </summary>
        public override void Install()
        {
            base.Install();
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        public override void Uninstall()
        {
            //discount requirements
            var discountRequirements = _discountService.GetAllDiscountRequirements()
            .Where(discountRequirement => discountRequirement.DiscountRequirementRuleSystemName == DiscountRequirementDefaults.SystemName);
            foreach (var discountRequirement in discountRequirements)
            {
                _discountService.DeleteDiscountRequirement(discountRequirement, false);
            }
            base.Uninstall();
        }

        #endregion
    }
}