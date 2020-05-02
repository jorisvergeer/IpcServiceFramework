﻿using JKang.IpcServiceFramework.Client;
using JKang.IpcServiceFramework.Testing.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace JKang.IpcServiceFramework.NamedPipeTests
{
    public class EdgeCaseTest
    {
        [Fact]
        public async Task ServerIsOff_Timeout()
        {
            int timeout = 1000; // 1s
            IIpcClient<ITestService> client = new ServiceCollection()
                .AddNamedPipeIpcClient<ITestService>(options =>
                {
                    options.PipeName = "inexisted-pipe";
                    options.ConnectionTimeout = timeout;
                })
                .BuildServiceProvider()
                .GetRequiredService<IIpcClient<ITestService>>();

            var sw = Stopwatch.StartNew();
            await Assert.ThrowsAsync<TimeoutException>(async () =>
            {
                string output = await client.InvokeAsync(x => x.StringType("abc"));
            });

            Assert.True(sw.ElapsedMilliseconds < timeout * 2); // makesure timeout works with marge
        }

        [Fact]
        public void ConnectionCancelled_Throw()
        {
            IIpcClient<ITestService> client = new ServiceCollection()
                .AddNamedPipeIpcClient<ITestService>(options =>
                {
                    options.PipeName = "inexisted-pipe";
                })
                .BuildServiceProvider()
                .GetRequiredService<IIpcClient<ITestService>>();

            using var cts = new CancellationTokenSource();

            Task.WaitAll(
                Task.Run(async () =>
                {
                    await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                    {
                        await client.InvokeAsync(x => x.ReturnVoid(), cts.Token);
                    });
                }),
                Task.Run(() => cts.CancelAfter(1000)));
        }
    }
}
