window.BENCHMARK_DATA = {
  "lastUpdate": 1773318794263,
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
      }
    ]
  }
}