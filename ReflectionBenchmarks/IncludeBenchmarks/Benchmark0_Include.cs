namespace ReflectionBenchmarks;

[MemoryDiagnoser]
public class Benchmark0_Include
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
        var result = _queryable
            .Include(x => x.Company)
            .ThenInclude(x => x.Country);

        return result;
    }

    [Benchmark]
    public object Custom1()
    {
        var result = _queryable
            .IncludeCustom1(x => x.Company)
            .ThenIncludeCustom1<Store, Company, Country>(x => x.Country);

        return result;
    }

    [Benchmark]
    public object Custom2()
    {
        var result = _queryable
            .IncludeCustom2(x => x.Company)
            .ThenIncludeCustom2<Store, Company, Country>(x => x.Country, typeof(Company));

        return result;
    }

    //[Benchmark]
    //public object Custom3()
    //{
    //    var result = _queryable
    //        .IncludeCustom3(x => x.Company)
    //        .ThenIncludeCustom3<Store, Company, Country>(x => x.Country);

    //    return result;
    //}

    [Benchmark]
    public object Custom4()
    {
        var result = _queryable
            .IncludeCustom4(x => x.Company)
            .ThenIncludeCustom4<Store, Company, Country>(x => x.Country);

        return result;
    }

    public static void PrintOutput()
    {
        var benchmark0 = new Benchmark0_Include();
        benchmark0.Setup();

        Console.WriteLine(((IQueryable<Store>)benchmark0.EFCore()).ToQueryString());
        Console.WriteLine();
        Console.WriteLine(((IQueryable<Store>)benchmark0.Custom1()).ToQueryString());
        Console.WriteLine();
        Console.WriteLine(((IQueryable<Store>)benchmark0.Custom2()).ToQueryString());
        //Console.WriteLine();
        //Console.WriteLine(((IQueryable<Store>)benchmark.Custom3()).ToQueryString());
        Console.WriteLine();
        Console.WriteLine(((IQueryable<Store>)benchmark0.Custom4()).ToQueryString());
    }
}
