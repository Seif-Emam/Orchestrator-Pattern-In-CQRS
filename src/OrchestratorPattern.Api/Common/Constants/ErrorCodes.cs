namespace OrchestratorPattern.Api.Common.Constants;

public static class ErrorCodes
{
    public const string ValidationError = "VALIDATION_ERROR";
    public const string NotFound = "NOT_FOUND";
    public const string ResourceConflict = "RESOURCE_CONFLICT";
    public const string DomainRuleViolation = "DOMAIN_RULE_VIOLATION";
    public const string InternalServerError = "INTERNAL_SERVER_ERROR";

    // Order specific
    public const string OrderNotFound = "ORDER_NOT_FOUND";
    public const string OrderInvalidState = "ORDER_INVALID_STATE";
    public const string CustomerNotFound = "CUSTOMER_NOT_FOUND";
    public const string EmptyOrderItems = "EMPTY_ORDER_ITEMS";

    // Inventory specific
    public const string ProductNotFound = "PRODUCT_NOT_FOUND";
    public const string ProductOutOfStock = "PRODUCT_OUT_OF_STOCK";
    public const string InsufficientInventory = "INSUFFICIENT_INVENTORY";

    // Payment specific
    public const string PaymentFailed = "PAYMENT_FAILED";
    public const string PaymentDeclined = "PAYMENT_DECLINED";
    public const string InvalidPaymentAmount = "INVALID_PAYMENT_AMOUNT";

    // Shipping specific
    public const string ShipmentFailed = "SHIPMENT_FAILED";
    public const string InvalidShippingAddress = "INVALID_SHIPPING_ADDRESS";

    // Checkout specific
    public const string CheckoutFailed = "CHECKOUT_FAILED";
}
