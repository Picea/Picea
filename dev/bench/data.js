window.BENCHMARK_DATA = {
  "lastUpdate": 1775304218975,
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
          "id": "b7f19a2c48b8ee5ce91beafd3de470d696cd2d32",
          "message": "chore: Bump version to 1.0.0-rc.2",
          "timestamp": "2026-03-13T15:47:37+01:00",
          "tree_id": "f8afc56b0d3c0b10af44c4cfcff0cb218b33dcf5",
          "url": "https://github.com/Picea/Picea/commit/b7f19a2c48b8ee5ce91beafd3de470d696cd2d32"
        },
        "date": 1773413301393,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_Single",
            "value": 6546.8125,
            "unit": "ns",
            "range": "± 204.31071174924693"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_WithObserver",
            "value": 6659.564516129032,
            "unit": "ns",
            "range": "± 620.6872209311298"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_Batch_100",
            "value": 34593.1875,
            "unit": "ns",
            "range": "± 612.9987731091583"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_WithFeedback",
            "value": 8015.816326530612,
            "unit": "ns",
            "range": "± 650.1410409540057"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_ComposedObserver",
            "value": 6669.387096774193,
            "unit": "ns",
            "range": "± 210.93437801353522"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Handle_Accept",
            "value": 6143.615384615385,
            "unit": "ns",
            "range": "± 48.568745885837714"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Handle_Reject",
            "value": 4007.21,
            "unit": "ns",
            "range": "± 406.480963436621"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Dispatch_Single",
            "value": 4563.357142857143,
            "unit": "ns",
            "range": "± 69.03467037862724"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Dispatch_WithFeedback",
            "value": 6636.8125,
            "unit": "ns",
            "range": "± 193.6235851902745"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Handle_Accept",
            "value": 6486.412371134021,
            "unit": "ns",
            "range": "± 831.6083281882328"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Handle_Reject",
            "value": 3736.1285714285714,
            "unit": "ns",
            "range": "± 186.79252393479138"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Dispatch_Single",
            "value": 3990.5,
            "unit": "ns",
            "range": "± 40.87269165441546"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Dispatch_WithFeedback",
            "value": 5967.857142857143,
            "unit": "ns",
            "range": "± 101.83157825385175"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Handle_Accept",
            "value": 4883.384615384615,
            "unit": "ns",
            "range": "± 68.32098074717905"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Handle_Reject",
            "value": 3100.6428571428573,
            "unit": "ns",
            "range": "± 58.496031611432535"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Dispatch_Single",
            "value": 2037.7692307692307,
            "unit": "ns",
            "range": "± 31.339947474306776"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Dispatch_WithFeedback",
            "value": 5202.586956521739,
            "unit": "ns",
            "range": "± 175.0584443470182"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Handle_Accept",
            "value": 4044.4666666666667,
            "unit": "ns",
            "range": "± 74.38593056933996"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Handle_Reject",
            "value": 1255.7,
            "unit": "ns",
            "range": "± 26.764048380510108"
          }
        ]
      },
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
          "id": "bf305207868323f8c4127093b7b5613c5ff20f51",
          "message": "test: Migrate Picea.Tests to TUnit (#8)\n\n* test: migrate Picea.Tests to TUnit\n\n* chore: Trigger PR validation rerun",
          "timestamp": "2026-03-14T23:29:56+01:00",
          "tree_id": "2b3da11e4db19c7fc1f4ea87ed8677552b426217",
          "url": "https://github.com/Picea/Picea/commit/bf305207868323f8c4127093b7b5613c5ff20f51"
        },
        "date": 1773527442630,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_Single",
            "value": 5728.846153846154,
            "unit": "ns",
            "range": "± 76.64185774741085"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_WithObserver",
            "value": 5843.028571428571,
            "unit": "ns",
            "range": "± 195.90190908511659"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_Batch_100",
            "value": 38046.61538461538,
            "unit": "ns",
            "range": "± 554.9742544271068"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_WithFeedback",
            "value": 8422.132075471698,
            "unit": "ns",
            "range": "± 373.26274710243416"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_ComposedObserver",
            "value": 6599.927835051546,
            "unit": "ns",
            "range": "± 486.6814120359496"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Handle_Accept",
            "value": 6460.7307692307695,
            "unit": "ns",
            "range": "± 80.4167829147559"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Handle_Reject",
            "value": 4195.656565656565,
            "unit": "ns",
            "range": "± 450.572949576565"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Dispatch_Single",
            "value": 5235.791666666667,
            "unit": "ns",
            "range": "± 139.0095333504823"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Dispatch_WithFeedback",
            "value": 6578.272727272727,
            "unit": "ns",
            "range": "± 191.3072255442918"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Handle_Accept",
            "value": 5442.857142857143,
            "unit": "ns",
            "range": "± 81.19099717319662"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Handle_Reject",
            "value": 3939.785714285714,
            "unit": "ns",
            "range": "± 59.89372456074377"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Dispatch_Single",
            "value": 4647.243243243243,
            "unit": "ns",
            "range": "± 243.1139769236616"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Dispatch_WithFeedback",
            "value": 6213.133333333333,
            "unit": "ns",
            "range": "± 108.5869412476648"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Handle_Accept",
            "value": 4987.078947368421,
            "unit": "ns",
            "range": "± 104.8058076155206"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Handle_Reject",
            "value": 3171.6612903225805,
            "unit": "ns",
            "range": "± 153.9051169856719"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Dispatch_Single",
            "value": 1907.7307692307693,
            "unit": "ns",
            "range": "± 27.526211284742903"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Dispatch_WithFeedback",
            "value": 5517.623711340206,
            "unit": "ns",
            "range": "± 537.4676009175646"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Handle_Accept",
            "value": 4196.428571428572,
            "unit": "ns",
            "range": "± 61.749449434748044"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Handle_Reject",
            "value": 1300.5862068965516,
            "unit": "ns",
            "range": "± 45.20707691231169"
          }
        ]
      },
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
          "id": "619383147ee78655a537e987d198f3ae641bed07",
          "message": "fix: synchronize runtime state and event snapshots (#15)",
          "timestamp": "2026-03-16T12:01:34+01:00",
          "tree_id": "44b26512285155af207a8482ba6cb255d2b8c745",
          "url": "https://github.com/Picea/Picea/commit/619383147ee78655a537e987d198f3ae641bed07"
        },
        "date": 1773658944627,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_Single",
            "value": 6740.273684210526,
            "unit": "ns",
            "range": "± 454.27937889292974"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_WithObserver",
            "value": 6569.148936170212,
            "unit": "ns",
            "range": "± 258.00918240513346"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_Batch_100",
            "value": 41430.30412371134,
            "unit": "ns",
            "range": "± 6204.705198206395"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_WithFeedback",
            "value": 8929.825,
            "unit": "ns",
            "range": "± 324.72906951756505"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_ComposedObserver",
            "value": 6719.62,
            "unit": "ns",
            "range": "± 181.14159286775268"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Handle_Accept",
            "value": 6733.367346938776,
            "unit": "ns",
            "range": "± 509.84037602555395"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Handle_Reject",
            "value": 3886.1428571428573,
            "unit": "ns",
            "range": "± 177.4294665054697"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Dispatch_Single",
            "value": 5451.526315789473,
            "unit": "ns",
            "range": "± 444.2926925356048"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Dispatch_WithFeedback",
            "value": 6704.375,
            "unit": "ns",
            "range": "± 244.9370602043277"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Handle_Accept",
            "value": 5865.788888888889,
            "unit": "ns",
            "range": "± 230.62214494140355"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Handle_Reject",
            "value": 3905.394366197183,
            "unit": "ns",
            "range": "± 192.51949651719798"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Dispatch_Single",
            "value": 4796.494845360825,
            "unit": "ns",
            "range": "± 441.0399434790302"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Dispatch_WithFeedback",
            "value": 6269.0641025641025,
            "unit": "ns",
            "range": "± 224.7981310645052"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Handle_Accept",
            "value": 5282.857142857143,
            "unit": "ns",
            "range": "± 95.22815286931223"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Handle_Reject",
            "value": 3434.690476190476,
            "unit": "ns",
            "range": "± 86.92331047976661"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Dispatch_Single",
            "value": 2232.917525773196,
            "unit": "ns",
            "range": "± 213.64757146715814"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Dispatch_WithFeedback",
            "value": 5870.090909090909,
            "unit": "ns",
            "range": "± 627.2042288700151"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Handle_Accept",
            "value": 4368.049180327869,
            "unit": "ns",
            "range": "± 209.76998722644666"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Handle_Reject",
            "value": 1524.9864864864865,
            "unit": "ns",
            "range": "± 58.78237718796227"
          }
        ]
      },
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
          "id": "747f57acd57581a92b5ad201877671a21d255315",
          "message": "fix: release decider gate exactly once (#16)",
          "timestamp": "2026-03-16T12:25:13+01:00",
          "tree_id": "4427910b8cf553d559d253abbc5e290f48fce9eb",
          "url": "https://github.com/Picea/Picea/commit/747f57acd57581a92b5ad201877671a21d255315"
        },
        "date": 1773660362215,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_Single",
            "value": 6868.743902439024,
            "unit": "ns",
            "range": "± 377.46161806269635"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_WithObserver",
            "value": 6511.884615384615,
            "unit": "ns",
            "range": "± 279.00050276475565"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_Batch_100",
            "value": 36705.791666666664,
            "unit": "ns",
            "range": "± 946.0768412199685"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_WithFeedback",
            "value": 7417.259259259259,
            "unit": "ns",
            "range": "± 155.86991434985202"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_ComposedObserver",
            "value": 5701.382352941177,
            "unit": "ns",
            "range": "± 180.57853402847095"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Handle_Accept",
            "value": 6470.5625,
            "unit": "ns",
            "range": "± 208.7999409065545"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Handle_Reject",
            "value": 3877.1195652173915,
            "unit": "ns",
            "range": "± 233.52736349966702"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Dispatch_Single",
            "value": 4689.692307692308,
            "unit": "ns",
            "range": "± 74.60047432309509"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Dispatch_WithFeedback",
            "value": 7663.911764705882,
            "unit": "ns",
            "range": "± 245.4891366660323"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Handle_Accept",
            "value": 6719.857142857143,
            "unit": "ns",
            "range": "± 198.99706200803743"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Handle_Reject",
            "value": 3955.6969696969695,
            "unit": "ns",
            "range": "± 121.14182928712239"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Dispatch_Single",
            "value": 4831.257731958763,
            "unit": "ns",
            "range": "± 404.2773202052056"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Dispatch_WithFeedback",
            "value": 7056.642857142857,
            "unit": "ns",
            "range": "± 716.6135890997841"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Handle_Accept",
            "value": 6087.105263157895,
            "unit": "ns",
            "range": "± 140.67215421241062"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Handle_Reject",
            "value": 3600.4690721649486,
            "unit": "ns",
            "range": "± 449.88687128007183"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Dispatch_Single",
            "value": 2683.3453608247423,
            "unit": "ns",
            "range": "± 293.4885127013132"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Dispatch_WithFeedback",
            "value": 5523,
            "unit": "ns",
            "range": "± 74.1160024433443"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Handle_Accept",
            "value": 4471.933333333333,
            "unit": "ns",
            "range": "± 137.01923684055393"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Handle_Reject",
            "value": 1547.04,
            "unit": "ns",
            "range": "± 47.54951804873175"
          }
        ]
      },
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
          "id": "d4a487e76425f19643708b59d7941e944034439f",
          "message": "chore: address copilot review feedback (#17)",
          "timestamp": "2026-03-16T13:19:40+01:00",
          "tree_id": "d5653fc97e2e0e28038b268815718c1ed3d0f526",
          "url": "https://github.com/Picea/Picea/commit/d4a487e76425f19643708b59d7941e944034439f"
        },
        "date": 1773663627704,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_Single",
            "value": 6427.043010752688,
            "unit": "ns",
            "range": "± 499.97127991381956"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_WithObserver",
            "value": 6784.538461538462,
            "unit": "ns",
            "range": "± 108.60449298917563"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_Batch_100",
            "value": 46276.88043478261,
            "unit": "ns",
            "range": "± 2599.802612297653"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_WithFeedback",
            "value": 8503.67441860465,
            "unit": "ns",
            "range": "± 317.2019576690199"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_ComposedObserver",
            "value": 6319.239583333333,
            "unit": "ns",
            "range": "± 632.1800000964361"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Handle_Accept",
            "value": 6723.752688172043,
            "unit": "ns",
            "range": "± 561.1593909800374"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Handle_Reject",
            "value": 3779.7820512820513,
            "unit": "ns",
            "range": "± 138.09682886674094"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Dispatch_Single",
            "value": 4687.448717948718,
            "unit": "ns",
            "range": "± 171.56983367498046"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Dispatch_WithFeedback",
            "value": 6698.581395348837,
            "unit": "ns",
            "range": "± 238.06143990456596"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Handle_Accept",
            "value": 5658.615384615385,
            "unit": "ns",
            "range": "± 81.68388096960385"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Handle_Reject",
            "value": 4329.395833333333,
            "unit": "ns",
            "range": "± 488.19613140550194"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Dispatch_Single",
            "value": 4692.020833333333,
            "unit": "ns",
            "range": "± 575.4557915100482"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Dispatch_WithFeedback",
            "value": 6278.928571428572,
            "unit": "ns",
            "range": "± 103.08824769675161"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Handle_Accept",
            "value": 5229.681818181818,
            "unit": "ns",
            "range": "± 130.68013889378412"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Handle_Reject",
            "value": 3619.4574468085107,
            "unit": "ns",
            "range": "± 433.85150982606643"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Dispatch_Single",
            "value": 2387.3571428571427,
            "unit": "ns",
            "range": "± 307.8171903588432"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Dispatch_WithFeedback",
            "value": 5457.533333333334,
            "unit": "ns",
            "range": "± 78.2266548166897"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Handle_Accept",
            "value": 4578.692307692308,
            "unit": "ns",
            "range": "± 58.9100792385941"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Handle_Reject",
            "value": 1701.0263157894738,
            "unit": "ns",
            "range": "± 98.93121835352217"
          }
        ]
      },
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
          "id": "990bd235902fb4a259668635b2b6e1453c1ac459",
          "message": "build: pin tooling and gate vulnerable publishes (#18)",
          "timestamp": "2026-03-16T13:35:51+01:00",
          "tree_id": "19f63981f8701271da372a49c3f0bdcc8276bc6a",
          "url": "https://github.com/Picea/Picea/commit/990bd235902fb4a259668635b2b6e1453c1ac459"
        },
        "date": 1773664596409,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_Single",
            "value": 6463.112903225807,
            "unit": "ns",
            "range": "± 203.5186604743907"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_WithObserver",
            "value": 5560.433333333333,
            "unit": "ns",
            "range": "± 72.4090233787659"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_Batch_100",
            "value": 36371.86666666667,
            "unit": "ns",
            "range": "± 656.1062486547506"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_WithFeedback",
            "value": 7170.923076923077,
            "unit": "ns",
            "range": "± 88.23213845538517"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_ComposedObserver",
            "value": 6581.474358974359,
            "unit": "ns",
            "range": "± 240.07339745865522"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Handle_Accept",
            "value": 6301.642857142857,
            "unit": "ns",
            "range": "± 93.34044975486009"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Handle_Reject",
            "value": 3903.9333333333334,
            "unit": "ns",
            "range": "± 46.898776509576855"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Dispatch_Single",
            "value": 5090.99494949495,
            "unit": "ns",
            "range": "± 351.65263976808814"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Dispatch_WithFeedback",
            "value": 6648.2307692307695,
            "unit": "ns",
            "range": "± 69.87864939325632"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Handle_Accept",
            "value": 6505.230158730159,
            "unit": "ns",
            "range": "± 306.206561440687"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Handle_Reject",
            "value": 3661.0833333333335,
            "unit": "ns",
            "range": "± 115.80633464045542"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Dispatch_Single",
            "value": 3954.5588235294117,
            "unit": "ns",
            "range": "± 43.84414240841543"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Dispatch_WithFeedback",
            "value": 5987.866666666667,
            "unit": "ns",
            "range": "± 72.59758621200517"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Handle_Accept",
            "value": 5299.214285714285,
            "unit": "ns",
            "range": "± 61.95212614645226"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Handle_Reject",
            "value": 3438.1,
            "unit": "ns",
            "range": "± 75.90776952120389"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Dispatch_Single",
            "value": 2215.3214285714284,
            "unit": "ns",
            "range": "± 61.41664243716004"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Dispatch_WithFeedback",
            "value": 5951.41,
            "unit": "ns",
            "range": "± 528.7166489231115"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Handle_Accept",
            "value": 4610.857142857143,
            "unit": "ns",
            "range": "± 52.818786201731065"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Handle_Reject",
            "value": 1639.4666666666667,
            "unit": "ns",
            "range": "± 32.48706702381001"
          }
        ]
      },
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
          "id": "5c12a37563ecf14772141821a4ca14028dd01b69",
          "message": "fix: enforce non-null result and option contracts (#19)",
          "timestamp": "2026-03-16T13:47:54+01:00",
          "tree_id": "ae7454d495a2445558c0001e0a042efeae441f65",
          "url": "https://github.com/Picea/Picea/commit/5c12a37563ecf14772141821a4ca14028dd01b69"
        },
        "date": 1773665320628,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_Single",
            "value": 6942.710144927536,
            "unit": "ns",
            "range": "± 346.0168349121553"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_WithObserver",
            "value": 6791.239583333333,
            "unit": "ns",
            "range": "± 421.7715706221648"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_Batch_100",
            "value": 38642.2,
            "unit": "ns",
            "range": "± 648.8127839149199"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_WithFeedback",
            "value": 7569.928571428572,
            "unit": "ns",
            "range": "± 133.3234726207218"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_ComposedObserver",
            "value": 7138.34,
            "unit": "ns",
            "range": "± 194.49329037270155"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Handle_Accept",
            "value": 7180.072164948454,
            "unit": "ns",
            "range": "± 544.0645689204906"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Handle_Reject",
            "value": 3768.191489361702,
            "unit": "ns",
            "range": "± 155.62505642364926"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Dispatch_Single",
            "value": 5665.727272727273,
            "unit": "ns",
            "range": "± 138.56000309928714"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Dispatch_WithFeedback",
            "value": 8140.37037037037,
            "unit": "ns",
            "range": "± 192.65057011398164"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Handle_Accept",
            "value": 6596.041666666667,
            "unit": "ns",
            "range": "± 674.5762945679181"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Handle_Reject",
            "value": 4040.2736842105264,
            "unit": "ns",
            "range": "± 326.50578966243575"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Dispatch_Single",
            "value": 4663.886075949367,
            "unit": "ns",
            "range": "± 242.46242042109475"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Dispatch_WithFeedback",
            "value": 6815.5,
            "unit": "ns",
            "range": "± 230.24024719534034"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Handle_Accept",
            "value": 5663.846153846154,
            "unit": "ns",
            "range": "± 156.2458811764822"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Handle_Reject",
            "value": 3516.6,
            "unit": "ns",
            "range": "± 52.1409022189924"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Dispatch_Single",
            "value": 2602.4591836734694,
            "unit": "ns",
            "range": "± 278.6900568757138"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Dispatch_WithFeedback",
            "value": 6128.037037037037,
            "unit": "ns",
            "range": "± 175.0530648077191"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Handle_Accept",
            "value": 5026,
            "unit": "ns",
            "range": "± 106.8649505079136"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Handle_Reject",
            "value": 1640.95,
            "unit": "ns",
            "range": "± 42.28036124926584"
          }
        ]
      },
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
          "id": "bac5ee55ca0623add5c777e67cfbf93cc566b074",
          "message": "chore: add Squad AI team for Picea (#24)\n\n- Add Squad agent system (.squad/) with team roster, routing, decisions\n- Add squad agent charters and onboarded histories for all 9 agents\n- Add squad GitHub workflows (heartbeat, triage, issue-assign, label-sync)\n- Add squad governance file (.github/agents/squad.agent.md)\n- Add squad MCP config (.copilot/mcp-config.json)\n- Add .gitattributes merge=union drivers for append-only squad files\n- Update .gitignore to exclude squad runtime state (logs, inbox)",
          "timestamp": "2026-03-30T17:13:02+02:00",
          "tree_id": "f73dce9ae6fa45141b00b6d49e28217efa8765c9",
          "url": "https://github.com/Picea/Picea/commit/bac5ee55ca0623add5c777e67cfbf93cc566b074"
        },
        "date": 1774883638412,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_Single",
            "value": 6655.227272727273,
            "unit": "ns",
            "range": "± 402.7306429139727"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_WithObserver",
            "value": 6180.55376344086,
            "unit": "ns",
            "range": "± 457.7138693643756"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_Batch_100",
            "value": 38974.4375,
            "unit": "ns",
            "range": "± 741.1870630954105"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_WithFeedback",
            "value": 8883.031578947368,
            "unit": "ns",
            "range": "± 952.8030254859289"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_ComposedObserver",
            "value": 6797.065934065934,
            "unit": "ns",
            "range": "± 395.0871578159208"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Handle_Accept",
            "value": 7792.175675675676,
            "unit": "ns",
            "range": "± 274.0505888065655"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Handle_Reject",
            "value": 4723.0625,
            "unit": "ns",
            "range": "± 154.31031523231613"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Dispatch_Single",
            "value": 5690.894736842105,
            "unit": "ns",
            "range": "± 205.5470998136942"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Dispatch_WithFeedback",
            "value": 8175.333333333333,
            "unit": "ns",
            "range": "± 123.11531566942007"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Handle_Accept",
            "value": 5898.461538461538,
            "unit": "ns",
            "range": "± 85.29518879027837"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Handle_Reject",
            "value": 3706.6428571428573,
            "unit": "ns",
            "range": "± 111.0638774487438"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Dispatch_Single",
            "value": 4439.868421052632,
            "unit": "ns",
            "range": "± 96.94454917134374"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Dispatch_WithFeedback",
            "value": 7476.918918918919,
            "unit": "ns",
            "range": "± 259.63418478680705"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Handle_Accept",
            "value": 5543.928571428572,
            "unit": "ns",
            "range": "± 80.87855500279824"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Handle_Reject",
            "value": 3456.076923076923,
            "unit": "ns",
            "range": "± 54.28391649230543"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Dispatch_Single",
            "value": 2381.0470588235294,
            "unit": "ns",
            "range": "± 142.21704621912986"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Dispatch_WithFeedback",
            "value": 6253.78125,
            "unit": "ns",
            "range": "± 559.2574626587236"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Handle_Accept",
            "value": 4973.472222222223,
            "unit": "ns",
            "range": "± 174.20209710253388"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Handle_Reject",
            "value": 1604.9583333333333,
            "unit": "ns",
            "range": "± 45.06273806948309"
          }
        ]
      },
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
          "id": "069dc86f38f5880c8ca2fb890c28fbf996e4d3ef",
          "message": "refactor(decider): adopt validated DU and auth-context authorize pipeline (#25)\n\n* refactor(decider): adopt validated DU and auth-context authorize pipeline\n\n* docs(decider): align staged pipeline and authorization context guidance\n\n* test(tracing): stabilize dispatch span selection in parallel runs",
          "timestamp": "2026-04-04T13:34:49+02:00",
          "tree_id": "67e323ff3cf3d199cec48cbcb9640ad79e6848fe",
          "url": "https://github.com/Picea/Picea/commit/069dc86f38f5880c8ca2fb890c28fbf996e4d3ef"
        },
        "date": 1775302539000,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_Single",
            "value": 6049.302083333333,
            "unit": "ns",
            "range": "± 521.16526563162"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_WithObserver",
            "value": 7873.677215189873,
            "unit": "ns",
            "range": "± 419.15429994991626"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_Batch_100",
            "value": 38893.060975609755,
            "unit": "ns",
            "range": "± 1390.4115047132716"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_WithFeedback",
            "value": 8004.30701754386,
            "unit": "ns",
            "range": "± 355.1601971845387"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_ComposedObserver",
            "value": 7822.388888888889,
            "unit": "ns",
            "range": "± 572.6723408523623"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Handle_Accept",
            "value": 8165.66,
            "unit": "ns",
            "range": "± 223.96924640078007"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Handle_Reject",
            "value": 4313.103092783505,
            "unit": "ns",
            "range": "± 385.8275058379954"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Dispatch_Single",
            "value": 5471.177777777778,
            "unit": "ns",
            "range": "± 215.43595218753413"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Dispatch_WithFeedback",
            "value": 8123.784946236559,
            "unit": "ns",
            "range": "± 1134.633333317539"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Handle_Accept",
            "value": 6715.522222222222,
            "unit": "ns",
            "range": "± 384.80612301471126"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Handle_Reject",
            "value": 5512.634020618557,
            "unit": "ns",
            "range": "± 400.5947044932587"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Dispatch_Single",
            "value": 3663.0729166666665,
            "unit": "ns",
            "range": "± 270.429628740782"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Dispatch_WithFeedback",
            "value": 7386.343434343435,
            "unit": "ns",
            "range": "± 936.9172764391348"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Handle_Accept",
            "value": 6093.108108108108,
            "unit": "ns",
            "range": "± 212.78089719288764"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Handle_Reject",
            "value": 4735.6875,
            "unit": "ns",
            "range": "± 288.51738294799134"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Dispatch_Single",
            "value": 2369.550505050505,
            "unit": "ns",
            "range": "± 193.80851973319156"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Dispatch_WithFeedback",
            "value": 5986.540816326531,
            "unit": "ns",
            "range": "± 693.4952547926664"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Handle_Accept",
            "value": 6557.505154639175,
            "unit": "ns",
            "range": "± 587.8750243410467"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Handle_Reject",
            "value": 3862.4845360824743,
            "unit": "ns",
            "range": "± 256.4853030796819"
          }
        ]
      },
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
          "id": "3cb779d766e65f8ca93d786d7a6e903bcd9f32c0",
          "message": "chore(release): Bump to rc.3 and update changelog (#26)\n\n* chore(release): bump to rc.3 and document release notes\n\n* chore(release): Trigger PR validation rerun",
          "timestamp": "2026-04-04T13:51:02+02:00",
          "tree_id": "ec1fb9c63001802719ebf830a7aa02a645a7aa9d",
          "url": "https://github.com/Picea/Picea/commit/3cb779d766e65f8ca93d786d7a6e903bcd9f32c0"
        },
        "date": 1775303513429,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_Single",
            "value": 6750.03,
            "unit": "ns",
            "range": "± 446.9140756455329"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_WithObserver",
            "value": 5799.236842105263,
            "unit": "ns",
            "range": "± 110.0241600262128"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_Batch_100",
            "value": 61270.86666666667,
            "unit": "ns",
            "range": "± 1072.1401738489947"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_WithFeedback",
            "value": 7468.142857142857,
            "unit": "ns",
            "range": "± 73.0583087917896"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_ComposedObserver",
            "value": 6684.7,
            "unit": "ns",
            "range": "± 274.393375448089"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Handle_Accept",
            "value": 8563.727272727272,
            "unit": "ns",
            "range": "± 209.83030146109834"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Handle_Reject",
            "value": 3771.03125,
            "unit": "ns",
            "range": "± 113.52319199603112"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Dispatch_Single",
            "value": 4658.0625,
            "unit": "ns",
            "range": "± 72.03699859562909"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Dispatch_WithFeedback",
            "value": 6813.142857142857,
            "unit": "ns",
            "range": "± 86.75416828190859"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Handle_Accept",
            "value": 7943.901960784314,
            "unit": "ns",
            "range": "± 334.5614595198892"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Handle_Reject",
            "value": 3957.4,
            "unit": "ns",
            "range": "± 126.12646909707951"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Dispatch_Single",
            "value": 4591.788659793814,
            "unit": "ns",
            "range": "± 434.84230759463463"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Dispatch_WithFeedback",
            "value": 6680.015625,
            "unit": "ns",
            "range": "± 317.3991274423899"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Handle_Accept",
            "value": 5942.075,
            "unit": "ns",
            "range": "± 201.59522627386357"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Handle_Reject",
            "value": 4146.893939393939,
            "unit": "ns",
            "range": "± 139.73522716953377"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Dispatch_Single",
            "value": 2381.529411764706,
            "unit": "ns",
            "range": "± 141.42334186636185"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Dispatch_WithFeedback",
            "value": 6141,
            "unit": "ns",
            "range": "± 99.48282933920473"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Handle_Accept",
            "value": 5691.777777777777,
            "unit": "ns",
            "range": "± 90.44306337369551"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Handle_Reject",
            "value": 2994.2957746478874,
            "unit": "ns",
            "range": "± 147.93728346510284"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "49699333+dependabot[bot]@users.noreply.github.com",
            "name": "dependabot[bot]",
            "username": "dependabot[bot]"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "a534b4df434c6eff018ef4b7a19ed80509e6fa42",
          "message": "Bump the minor-and-patch group with 1 update (#23)\n\nBumps TUnit from 1.19.57 to 1.23.7\n\n---\nupdated-dependencies:\n- dependency-name: TUnit\n  dependency-version: 1.23.7\n  dependency-type: direct:production\n  update-type: version-update:semver-minor\n  dependency-group: minor-and-patch\n...\n\nSigned-off-by: dependabot[bot] <support@github.com>\nCo-authored-by: dependabot[bot] <49699333+dependabot[bot]@users.noreply.github.com>",
          "timestamp": "2026-04-04T14:02:47+02:00",
          "tree_id": "0da9c76f0a0e6e8e12c1a91eef1123a9041793d5",
          "url": "https://github.com/Picea/Picea/commit/a534b4df434c6eff018ef4b7a19ed80509e6fa42"
        },
        "date": 1775304218650,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_Single",
            "value": 6645.117021276596,
            "unit": "ns",
            "range": "± 537.1073030372778"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_WithObserver",
            "value": 6633.607594936709,
            "unit": "ns",
            "range": "± 356.42201354075934"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_Batch_100",
            "value": 40811.769230769234,
            "unit": "ns",
            "range": "± 357.6819243047641"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_WithFeedback",
            "value": 8900.746575342466,
            "unit": "ns",
            "range": "± 452.7554460320936"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Dispatch_ComposedObserver",
            "value": 6324.442105263158,
            "unit": "ns",
            "range": "± 388.89747912800425"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Handle_Accept",
            "value": 7299.6875,
            "unit": "ns",
            "range": "± 143.8993716687695"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Handle_Reject",
            "value": 4095.32,
            "unit": "ns",
            "range": "± 425.315376271455"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Dispatch_Single",
            "value": 5540.384615384615,
            "unit": "ns",
            "range": "± 238.26267872329097"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Dispatch_WithFeedback",
            "value": 6849.433333333333,
            "unit": "ns",
            "range": "± 127.31932109389182"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Handle_Accept",
            "value": 7610.878787878788,
            "unit": "ns",
            "range": "± 245.42307317871487"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Safe_NoTrack_Handle_Reject",
            "value": 3854.909090909091,
            "unit": "ns",
            "range": "± 120.05711235604798"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Dispatch_Single",
            "value": 4262.583333333333,
            "unit": "ns",
            "range": "± 59.23214473321872"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Dispatch_WithFeedback",
            "value": 6875.775510204082,
            "unit": "ns",
            "range": "± 541.4724965764246"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Handle_Accept",
            "value": 6194.153846153846,
            "unit": "ns",
            "range": "± 101.39596158447843"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Lean_Handle_Reject",
            "value": 3444.0454545454545,
            "unit": "ns",
            "range": "± 86.93567644714952"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Dispatch_Single",
            "value": 2307.413043478261,
            "unit": "ns",
            "range": "± 46.62883041731752"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Dispatch_WithFeedback",
            "value": 5967.190476190476,
            "unit": "ns",
            "range": "± 132.68482168191622"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Handle_Accept",
            "value": 5927.040404040404,
            "unit": "ns",
            "range": "± 576.8834975793874"
          },
          {
            "name": "Picea.Benchmarks.PiceaBenchmarks.Rec_Lean_Handle_Reject",
            "value": 3265.4747474747473,
            "unit": "ns",
            "range": "± 385.53494093575165"
          }
        ]
      }
    ]
  }
}