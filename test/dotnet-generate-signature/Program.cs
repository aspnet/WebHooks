// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.AspNetCore.WebHooks
{
    internal static class Program
    {
        public static int Main(string[] args)
        {
            var assemblyName = typeof(Program).Assembly.GetName().Name;
            var application = new CommandLineApplication
            {
                Name = assemblyName,
            };

            application.Command(
                "dropbox",
                configuration =>
                {
                    var (filenameArgument, secretKeyOption) = Configure(configuration, "Dropbox");
                    configuration.OnExecute(
                        async () =>
                        {
                            if (string.IsNullOrEmpty(filenameArgument.Value))
                            {
                                throw new CommandParsingException(
                                    configuration,
                                    $"Missing value for argument '{filenameArgument.Name}'.");
                            }

                            var hash = await DropboxSignatureGenerator.Compute(
                                filenameArgument.Value,
                                secretKeyOption.Value());

                            await configuration.Out.WriteLineAsync(hash);
                            return 0;
                        });
                });

            application.Command(
                "github",
                configuration =>
                {
                    var (filenameArgument, secretKeyOption) = Configure(configuration, "GitHub");
                    configuration.OnExecute(
                        async () =>
                        {
                            if (string.IsNullOrEmpty(filenameArgument.Value))
                            {
                                throw new CommandParsingException(
                                    configuration,
                                    $"Missing value for argument '{filenameArgument.Name}'.");
                            }

                            var hash = await GitHubSignatureGenerator.Compute(
                                filenameArgument.Value,
                                secretKeyOption.Value());

                            await configuration.Out.WriteLineAsync(hash);
                            return 0;
                        });
                });

            application.Command(
                "pusher",
                configuration =>
                {
                    var (filenameArgument, secretKeyOption) = Configure(configuration, "Pusher");
                    configuration.OnExecute(
                        async () =>
                        {
                            if (string.IsNullOrEmpty(filenameArgument.Value))
                            {
                                throw new CommandParsingException(
                                    configuration,
                                    $"Missing value for argument '{filenameArgument.Name}'.");
                            }

                            var hash = await PusherSignatureGenerator.Compute(
                                filenameArgument.Value,
                                secretKeyOption.Value());

                            await configuration.Out.WriteLineAsync(hash);
                            return 0;
                        });
                });

            application.Command(
                "stripe",
                configuration =>
                {
                    var (filenameArgument, secretKeyOption) = Configure(configuration, "Stripe");
                    var timestampOption = configuration.Option(
                        "-t|--timestamp",
                        "The Unix timestamp to included in the hash.",
                        CommandOptionType.SingleValue);

                    configuration.OnExecute(
                        async () =>
                        {
                            if (string.IsNullOrEmpty(filenameArgument.Value))
                            {
                                throw new CommandParsingException(
                                    configuration,
                                    $"Missing value for argument '{filenameArgument.Name}'.");
                            }

                            var hash = await StripeSignatureGenerator.Compute(
                                filenameArgument.Value,
                                secretKeyOption.Value(),
                                timestampOption.Value());

                            await configuration.Out.WriteLineAsync(hash);
                            return 0;
                        });
                });

            application.Command(
                "trello",
                configuration =>
                {
                    var (filenameArgument, secretKeyOption) = Configure(configuration, "Trello");
                    var urlOption = configuration.Option(
                        "-u|--url",
                        "The URL to include in the hash.",
                        CommandOptionType.SingleValue);

                    configuration.OnExecute(
                        async () =>
                        {
                            if (string.IsNullOrEmpty(filenameArgument.Value))
                            {
                                throw new CommandParsingException(
                                    configuration,
                                    $"Missing value for argument '{filenameArgument.Name}'.");
                            }

                            var hash = await TrelloSignatureGenerator.Compute(
                                filenameArgument.Value,
                                secretKeyOption.Value(),
                                urlOption.Value());

                            await configuration.Out.WriteLineAsync(hash);
                            return 0;
                        });
                });

            application.HelpOption("-?|-h|--help");
            application.OnExecute(() =>
            {
                // Show help if no command was specified.
                application.ShowHelp();
                return 0;
            });

            try
            {
                return application.Execute(args);
            }
            catch (AggregateException aggregateException)
                when (aggregateException.InnerException is CommandParsingException parsingException)
            {
                application.Error.WriteLine($"{parsingException.GetType().Name}: {parsingException.Message}");
                application.ShowHelp(parsingException.Command.Name);

                return 1;
            }
            catch (AggregateException aggregateException)
            {
                var exception = aggregateException.InnerException;
                application.Error.WriteLine($"{exception.GetType().Name}: {exception.Message}");

                return 2;
            }
            catch (CommandParsingException parsingException)
            {
                application.Error.WriteLine($"{parsingException.GetType().Name}: {parsingException.Message}");
                application.ShowHelp(parsingException.Command.Name);

                return 3;
            }
            catch (Exception exception)
            {
                application.Error.WriteLine($"{exception.GetType().Name}: {exception.Message}");

                return 4;
            }
        }

        private static (CommandArgument, CommandOption) Configure(
            CommandLineApplication configuration,
            string receiverName)
        {
            var filenameArgument = configuration.Argument("[filename]", "The name of the file to hash.");
            var secretKeyOption = configuration.Option(
                "-s|--secretKey",
                "The key data used to initialize the hasher.",
                CommandOptionType.SingleValue);

            configuration.Description = $"Generate a {receiverName} signature for the [filename] file.";
            configuration.HelpOption("-?|-h|--help");

            return (filenameArgument, secretKeyOption);
        }
    }
}
