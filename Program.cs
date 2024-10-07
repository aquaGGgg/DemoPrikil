using System.Diagnostics.Metrics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});


var app = builder.Build();

app.UseCors("AllowAllOrigins");


List<order> repo = new List<order>();

foreach (var o in repo)
{
    o.EndDate = DateTime.Now;
    o.Status = "Завершено";
}

bool isUpdatedStatus = false;
string massage = "";

app.MapGet("/", () =>
{
    if (isUpdatedStatus)
    {
        string buffer = massage;
        isUpdatedStatus = false;
        massage = "";
        return Results.Json(new OrderUpdateStatusDTO(repo, buffer));
    }
    else
        return Results.Json(repo);
});
app.MapPost("/", (order o) => repo.Add(o));
app.MapPut("/{number}", (int number, OrderUpdateDTO dto) =>
{
    order buffer = repo.Find(o => o.Number == number);
    if (buffer.Status != dto.Status)
    {
        buffer.Status = dto.Status;
        isUpdatedStatus = true;
        massage += "Статус заявки номер " + buffer.Number + "Изменён\n";
        if (buffer.Status == "завершено")
            buffer.EndDate = DateTime.Now;
    }

    if (buffer == null)
        return Results.NotFound("пупуни нет");

    if (buffer.Master != dto.Master)
        buffer.Master = dto.Master;

    if (buffer.Description != dto.Description)
        buffer.Description = dto.Description;

    if (dto.Comment != null || dto.Comment != "")
        buffer.Comments.Add(dto.Comment);
    return Results.Json(buffer);
});
app.MapGet("/ {number}", (int number) => repo.Find(o => o.Number == number));
app.MapGet("/filter/{param}", (string param) => repo.FindAll(o =>
    o.Device == param ||
    o.ProblemType == param ||
    o.Description == param ||
    o.Client == param ||
    o.Status == param ||
    o.Master == param));
app.MapGet("/stats/ComplCount", () => repo.FindAll(o => o.Status == "Завершено").Count);
app.MapGet("/stats/ProblemTypes", () =>
{
    Dictionary<string, int> results = [];
    foreach (var o in repo)
        if (results.ContainsKey(o.ProblemType))
            results[o.ProblemType]++;
        else
            results[o.ProblemType] = 1;
    return results;
});
app.MapGet("/stats/avrg", () =>
{
    double timeSum = 0;
    int oCount = 0;
    foreach (var o in repo)
        if (o.Status == "Завершено")
        {
            timeSum += o.TimeInDay();
            oCount++;
        }
    return oCount > 0 ? timeSum / oCount : 0;
});
app.Run();

record class OrderUpdateDTO(string Status, string Description, string Master, string Comment);
record class OrderUpdateStatusDTO(List<order> repo, string massage);

class order
{
    int number;
    string device;
    string problemType;
    string description;
    string client;
    string status;
    string master;

    public order() { }

    public order(int number, int day, int month, int year, string device, string problemType, string description, string client, string status)
    {
        Number = number;
        StartDate = new DateTime(year, month, day);
        EndDate = null;
        Device = device;
        ProblemType = problemType;
        Description = description;
        Client = client;
        Status = status;
        Master = "Не назначен";
    }

    public int Number { get => number; set => number = value; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Device { get => device; set => device = value; }
    public string ProblemType { get => problemType; set => problemType = value; }
    public string Description { get => description; set => description = value; }
    public string Client { get => client; set => client = value; }
    public string Status { get => status; set => status = value; }
    public string Master { get => master; set => master = value; }
    public List<string> Comments { get; set; } = [];


    public double TimeInDay() => (EndDate - StartDate).Value.TotalDays;
}