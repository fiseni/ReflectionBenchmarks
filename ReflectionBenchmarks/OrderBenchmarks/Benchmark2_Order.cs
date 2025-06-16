namespace ReflectionBenchmarks;

[MemoryDiagnoser]
public class Benchmark2_Order
{
    private DbSet<Store> _queryable = default!;

    [GlobalSetup]
    public void Setup()
    {
        _queryable = new BenchmarkDbContext().Stores;
    }

    [Benchmark(Baseline = true)]
    public object EFCore()
    {
        return _queryable
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Id);
    }

    [Benchmark]
    public object Custom1()
    {
        return _queryable
            .OrderByCustom1(x => x.Name)
            .ThenByCustom1(x => x.Id);
    }

    [Benchmark]
    public object Custom2()
    {
        return _queryable
            .OrderByCustom2(x => x.Name)
            .ThenByCustom2(x => x.Id);
    }

    [Benchmark]
    public object Custom3()
    {
        return _queryable
            .OrderByCustom3(x => x.Name)
            .ThenByCustom3(x => x.Id);
    }

    [Benchmark]
    public object Custom4()
    {
        return _queryable
            .OrderByCustom4(x => x.Name)
            .ThenByCustom4(x => x.Id);
    }

    [Benchmark]
    public object Custom5()
    {
        return _queryable
            .OrderByCustom5(x => x.Name)
            .ThenByCustom5(x => x.Id);
    }

    [Benchmark]
    public object Custom6()
    {
        return _queryable
            .OrderByCustom6(x => x.Name)
            .ThenByCustom6(x => x.Id);
    }

    public static void PrintOutput()
    {
        var benchmark = new Benchmark2_Order();
        benchmark.Setup();

        Console.WriteLine(((IQueryable<Store>)benchmark.EFCore()).ToQueryString());
        Console.WriteLine();
        Console.WriteLine(((IQueryable<Store>)benchmark.Custom1()).ToQueryString());
        Console.WriteLine();
        Console.WriteLine(((IQueryable<Store>)benchmark.Custom2()).ToQueryString());
        Console.WriteLine();
        Console.WriteLine(((IQueryable<Store>)benchmark.Custom3()).ToQueryString());
        Console.WriteLine();
        Console.WriteLine(((IQueryable<Store>)benchmark.Custom4()).ToQueryString());
        Console.WriteLine();
        Console.WriteLine(((IQueryable<Store>)benchmark.Custom5()).ToQueryString());
        Console.WriteLine();
        Console.WriteLine(((IQueryable<Store>)benchmark.Custom6()).ToQueryString());
        Console.WriteLine();
    }
}
