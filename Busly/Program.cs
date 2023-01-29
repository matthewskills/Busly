using Busly;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();


BusData.setOptions(app.Configuration.GetValue<string>("NextBusURI"), app.Configuration.GetValue<string>("NextBusCredentials"), app.Configuration.GetValue<string>("DfTBusDataURI"), app.Configuration.GetValue<string>("DftBusDataAPIKey"));

app.MapGet("/stops", (Double lat, Double lng) => BusData.GetStops(lat,lng));
app.MapGet("/stopData", (String atcocode) => BusData.GetStopData(atcocode));
app.MapGet("/stopSearch", (String query) => BusData.StopSearch(query));
app.MapGet("/vehicleTrackingData", (String lineRef, String operatorRef) => BusData.GetVehicleTrackingData(lineRef,operatorRef));

app.Run();
