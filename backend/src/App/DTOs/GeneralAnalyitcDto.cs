namespace App.DTOs;


public class GeneralAnalyticsResponseDto
{
    public int Users { get; set; }
    public int Sellers { get; set; }
    public int Products { get; set; }
    public int Orders { get; set; }
    public int PendingOrders { get; set; }
    public int PendingPayments { get; set; }
    public decimal Revenue { get; set; }
    public int Reviews { get; set; }
}