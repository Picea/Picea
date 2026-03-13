window.BENCHMARK_DATA = {
  "lastUpdate": 1773412966656,
  "repoUrl": "https://github.com/Picea/Picea",
  "entries": {
    "Picea Benchmarks": [
      {
        "commit": {
          "author": {
            "email": "MCGPPeters@users.noreply.github.com",
            "name": "Maurice CGP Peters",
            "username": "MCGPPeters"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "18178e6cf9192578817f0d7a5e14c11ec7dd40b5",
          "message": "Merge pull request #1 from Picea/dependabot/github_actions/actions/checkout-6\n\nbuild(deps): bump actions/checkout from 4 to 6",
          "timestamp": "2026-03-12T13:23:56+01:00",
          "tree_id": "dfafb0393c6db747f862f077bdcc17d1b1f2d2fc",
          "url": "https://github.com/Picea/Picea/commit/18178e6cf9192578817f0d7a5e14c11ec7dd40b5"
        },
        "date": 1773318793578,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_Single",
            "value": 5690.416666666667,
            "unit": "ns",
            "range": "± 92.55903112297709"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_WithObserver",
            "value": 5775.777777777777,
            "unit": "ns",
            "range": "± 122.5962073857078"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_Batch_100",
            "value": 40027.42857142857,
            "unit": "ns",
            "range": "± 437.8309235109114"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_WithFeedback",
            "value": 8935.266666666666,
            "unit": "ns",
            "range": "± 138.08923960699508"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_ComposedObserver",
            "value": 6341.765306122449,
            "unit": "ns",
            "range": "± 457.1892060450816"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Handle_Accept",
            "value": 7399.928571428572,
            "unit": "ns",
            "range": "± 129.91444775799087"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Handle_Reject",
            "value": 3724.5833333333335,
            "unit": "ns",
            "range": "± 42.365205134173564"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Dispatch_Single",
            "value": 4661.058823529412,
            "unit": "ns",
            "range": "± 99.75060813613825"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Dispatch_WithFeedback",
            "value": 6531.433333333333,
            "unit": "ns",
            "range": "± 118.81225686330674"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Handle_Accept",
            "value": 5699.833333333333,
            "unit": "ns",
            "range": "± 58.16642788871716"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Handle_Reject",
            "value": 3609.259259259259,
            "unit": "ns",
            "range": "± 152.933600733793"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Dispatch_Single",
            "value": 4798.663265306122,
            "unit": "ns",
            "range": "± 375.6213531000225"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Dispatch_WithFeedback",
            "value": 6180.642857142857,
            "unit": "ns",
            "range": "± 67.87840615378371"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Handle_Accept",
            "value": 4870.64705882353,
            "unit": "ns",
            "range": "± 100.01308737889669"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Handle_Reject",
            "value": 3219.5714285714284,
            "unit": "ns",
            "range": "± 40.244377124138595"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "me@mauricepeters.dev",
            "name": "MCGPPeters",
            "username": "MCGPPeters"
          },
          "committer": {
            "email": "MCGPPeters@users.noreply.github.com",
            "name": "Maurice CGP Peters",
            "username": "MCGPPeters"
          },
          "distinct": true,
          "id": "341406f979c4b36151b97ee50212754bfa62fb21",
          "message": "docs: Add zero-alloc domain modeling guide and release prep updates\n\n- Zero-alloc domain modeling guide: explains boxing, abstract record\n  solution, three techniques, complete example, benchmark evidence\n- Runtime reference: InterpreterResult<TEvent> API docs + See Also link\n- Guides index: new entry for zero-alloc guide\n- README: interpreter example uses InterpreterResult, What's in the Box\n  table includes InterpreterResult<TEvent>, copyright 2025-2026\n- CHANGELOG: InterpreterResult entry, updated docs/benchmarks descriptions\n- Picea.csproj: copyright year 2025 → 2025-2026",
          "timestamp": "2026-03-13T15:41:58+01:00",
          "tree_id": "1540d8459f5a2b9801c98ad1436b590d85af9abb",
          "url": "https://github.com/Picea/Picea/commit/341406f979c4b36151b97ee50212754bfa62fb21"
        },
        "date": 1773412966016,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_Single",
            "value": 5506.285714285715,
            "unit": "ns",
            "range": "± 82.29910605312101"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_WithObserver",
            "value": 6480.96875,
            "unit": "ns",
            "range": "± 386.5005059743403"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_Batch_100",
            "value": 35179.09523809524,
            "unit": "ns",
            "range": "± 838.6063978268892"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_WithFeedback",
            "value": 8580.384615384615,
            "unit": "ns",
            "range": "± 143.29371843730533"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_ComposedObserver",
            "value": 5709.8,
            "unit": "ns",
            "range": "± 89.74583157848774"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Handle_Accept",
            "value": 6118.115384615385,
            "unit": "ns",
            "range": "± 86.8404077043424"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Handle_Reject",
            "value": 3900.6666666666665,
            "unit": "ns",
            "range": "± 76.48221631049081"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Dispatch_Single",
            "value": 4714,
            "unit": "ns",
            "range": "± 118.33427229674419"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Dispatch_WithFeedback",
            "value": 6628.466666666666,
            "unit": "ns",
            "range": "± 79.44348095763847"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Handle_Accept",
            "value": 6126.93,
            "unit": "ns",
            "range": "± 593.9628130896186"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Handle_Reject",
            "value": 3641.5510204081634,
            "unit": "ns",
            "range": "± 155.8030355428088"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Dispatch_Single",
            "value": 3987,
            "unit": "ns",
            "range": "± 40.00208327908269"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Dispatch_WithFeedback",
            "value": 5780,
            "unit": "ns",
            "range": "± 118.5939852325291"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Handle_Accept",
            "value": 5755.307692307692,
            "unit": "ns",
            "range": "± 158.2460790618887"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Handle_Reject",
            "value": 3287.733333333333,
            "unit": "ns",
            "range": "± 56.909033511720224"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Dispatch_Single",
            "value": 1979.6666666666667,
            "unit": "ns",
            "range": "± 48.20200549078154"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Dispatch_WithFeedback",
            "value": 5130.59375,
            "unit": "ns",
            "range": "± 83.62495996155351"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Handle_Accept",
            "value": 4185.923076923077,
            "unit": "ns",
            "range": "± 66.83744152601585"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Handle_Reject",
            "value": 1289.6666666666667,
            "unit": "ns",
            "range": "± 47.08215337754494"
          }
        ]
      }
    ]
  }
}