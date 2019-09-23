using BotCoreNET.CommandHandling;
using Discord;
using JSON;
using System;
using System.Collections.Generic;

namespace BotCoreNET.Helpers
{
    public static class EmbedHelper
    {
        public const string MESSAGECONTENT = "content";
        public const string EMBED = "embed";
        public const string TITLE = "title";
        public const string DESCRIPTION = "description";
        public const string URL = "url";
        public const string AUTHOR = "author";
        public const string NAME = "name";
        public const string ICON_URL = "icon_url";
        public const string THUMBNAIL = "thumbnail";
        public const string IMAGE = "image";
        public const string FOOTER = "footer";
        public const string TEXT = "text";
        public const string TIMESTAMP = "timestamp";
        public const string FIELDS = "fields";
        public const string VALUE = "value";
        public const string INLINE = "inline";
        public const string COLOR = "color";

        public const int MESSAGECONTENT_MAX = 2000;
        public const int EMBEDTITLE_MAX = 256;
        public const int EMBEDDESCRIPTION_MAX = 2048;
        public const int EMBEDFIELDCOUNT_MAX = 25;
        public const int EMBEDFIELDNAME_MAX = 256;
        public const int EMBEDFIELDVALUE_MAX = 1024;
        public const int EMBEDFOOTERTEXT_MAX = 2048;
        public const int EMBEDAUTHORNAME_MAX = 256;
        public const int EMBEDTOTALLENGTH_MAX = 6000;

        public static ArgumentParseResult TryParseEmbedFromJSONObject(JSONContainer json, out EmbedBuilder embed, out string messageContent)
        {
            embed = null;
            messageContent = null;

            json.TryGetField(MESSAGECONTENT, out JSONField messageContentJSON);
            json.TryGetField(EMBED, out JSONContainer embedJSON);

            if (messageContentJSON == null && embedJSON == null)
            {
                return new ArgumentParseResult("Neither message nor embed could be found!");
            }

            if ((messageContentJSON != null) && messageContentJSON.IsString)
            {
                if (!string.IsNullOrEmpty(messageContentJSON.String))
                {
                    if (messageContentJSON.String.Length > MESSAGECONTENT_MAX)
                    {
                        return new ArgumentParseResult($"The message content may not exceed {MESSAGECONTENT_MAX} characters!");
                    }
                    messageContent = messageContentJSON.String;
                }
            }
            else
            {
                messageContent = string.Empty;
            }

            if (embedJSON != null)
            {
                embed = new EmbedBuilder();

                // Parse TITLE, DESCRIPTION, TITLE_URL, TIMESTAMP

                if (embedJSON.TryGetField(TITLE, out string embedTitle))
                {
                    if (!string.IsNullOrEmpty(embedTitle))
                    {
                        if (embedTitle.Length > EMBEDTITLE_MAX)
                        {
                            return new ArgumentParseResult($"The embed title may not exceed {EMBEDTITLE_MAX} characters!");
                        }
                        embed.Title = embedTitle;
                    }
                }
                if (embedJSON.TryGetField(DESCRIPTION, out string embedDescription))
                {
                    if (!string.IsNullOrEmpty(embedDescription))
                    {
                        if (embedDescription.Length > EMBEDDESCRIPTION_MAX)
                        {
                            return new ArgumentParseResult($"The embed title may not exceed {EMBEDDESCRIPTION_MAX} characters!");
                        }
                        embed.Description = embedDescription;
                    }
                }
                if (embedJSON.TryGetField(URL, out string embedURL))
                {
                    if (!string.IsNullOrEmpty(embedURL))
                    {
                        if (Uri.IsWellFormedUriString(embedURL, UriKind.Absolute))
                        {
                            embed.Url = embedURL;
                        }
                        else
                        {
                            return new ArgumentParseResult("The url for the embed title is not a well formed url!");
                        }
                    }
                }
                if (embedJSON.TryGetField(TIMESTAMP, out string embedFooterTimestamp))
                {
                    if (!string.IsNullOrEmpty(embedFooterTimestamp))
                    {
                        if (DateTimeOffset.TryParse(embedFooterTimestamp, out DateTimeOffset timestamp))
                        {
                            embed.Timestamp = timestamp;
                        }
                        else
                        {
                            return new ArgumentParseResult("Could not parse the timestamp to a DateTimeOffset");
                        }
                    }
                }

                // Parse AUTHOR

                if (embedJSON.TryGetField(AUTHOR, out JSONContainer authorJSON))
                {
                    EmbedAuthorBuilder author = new EmbedAuthorBuilder();

                    if (authorJSON.TryGetField(NAME, out string authorName))
                    {
                        if (!string.IsNullOrEmpty(authorName))
                        {
                            if (authorName.Length > EMBEDAUTHORNAME_MAX)
                            {
                                return new ArgumentParseResult($"The embed author name may not exceed {EMBEDAUTHORNAME_MAX} characters!");
                            }
                            author.Name = authorName;
                        }
                    }
                    if (authorJSON.TryGetField(ICON_URL, out string authorIconUrl))
                    {
                        if (!string.IsNullOrEmpty(authorIconUrl))
                        {
                            author.IconUrl = authorIconUrl;
                        }
                    }
                    if (authorJSON.TryGetField(URL, out string authorUrl))
                    {
                        if (!string.IsNullOrEmpty(authorUrl))
                        {
                            author.Url = authorUrl;
                        }
                    }

                    embed.Author = author;
                }

                // Parse THUMBNAIL, IMAGE

                if (embedJSON.TryGetField(THUMBNAIL, out JSONContainer thumbnailJSON))
                {
                    if (thumbnailJSON.TryGetField(URL, out string thumbnailUrl))
                    {
                        if (Uri.IsWellFormedUriString(thumbnailUrl, UriKind.Absolute))
                        {
                            embed.ThumbnailUrl = thumbnailUrl;
                        }
                        else
                        {
                            return new ArgumentParseResult("The url for the embed thumbnail is not a well formed url!");
                        }
                    }
                }
                if (embedJSON.TryGetField(IMAGE, out JSONContainer imageJSON))
                {
                    if (imageJSON.TryGetField(URL, out string imageUrl))
                    {
                        if (Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute))
                        {
                            embed.ImageUrl = imageUrl;
                        }
                        else
                        {
                            return new ArgumentParseResult("The url for the embed image is not a well formed url!");
                        }
                    }
                }

                // Parse Color

                if (embedJSON.TryGetField(COLOR, out int color))
                {
                    Discord.Color embedColor = new Color((uint)color);
                    embed.Color = embedColor;
                }

                // Parse Footer

                if (embedJSON.TryGetField(FOOTER, out JSONContainer footerJSON))
                {
                    EmbedFooterBuilder footer = new EmbedFooterBuilder();

                    if (footerJSON.TryGetField(TEXT, out string footerText))
                    {
                        if (!string.IsNullOrEmpty(footerText))
                        {
                            if (footerText.Length > EMBEDFOOTERTEXT_MAX)
                            {
                                return new ArgumentParseResult($"The embed footer text may not exceed {EMBEDFOOTERTEXT_MAX} characters!");
                            }
                            footer.Text = footerText;
                        }
                    }
                    if (footerJSON.TryGetField(ICON_URL, out string footerIconUrl))
                    {
                        if (!string.IsNullOrEmpty(footerIconUrl))
                        {
                            if (Uri.IsWellFormedUriString(footerIconUrl, UriKind.Absolute))
                            {
                                footer.IconUrl = footerIconUrl;
                            }
                            else
                            {
                                return new ArgumentParseResult("The url for the embed footer icon is not a well formed url!");
                            }
                        }
                    }

                    embed.Footer = footer;
                }

                // Parse Fields

                if (embedJSON.TryGetField(FIELDS, out IReadOnlyList<JSONField> fieldsList))
                {
                    if (fieldsList.Count > EMBEDFIELDCOUNT_MAX)
                    {
                        return new ArgumentParseResult($"The embed can not have more than {EMBEDFIELDCOUNT_MAX} fields!");
                    }
                    foreach (JSONField fieldJSON in fieldsList)
                    {
                        if (fieldJSON.IsObject && fieldJSON.Container != null)
                        {
                            if (fieldJSON.Container.TryGetField(NAME, out string fieldName) && fieldJSON.Container.TryGetField(VALUE, out string fieldValue))
                            {
                                fieldJSON.Container.TryGetField(INLINE, out bool fieldInline, false);
                                if (fieldName != null && fieldValue != null)
                                {
                                    if (fieldName.Length > EMBEDFIELDNAME_MAX)
                                    {
                                        return new ArgumentParseResult($"A field name may not exceed {EMBEDFIELDNAME_MAX} characters!");
                                    }
                                    if (fieldValue.Length > EMBEDFIELDVALUE_MAX)
                                    {
                                        return new ArgumentParseResult($"A field value may not exceed {EMBEDFIELDVALUE_MAX} characters!");
                                    }
                                    embed.AddField(fieldName, fieldValue, fieldInline);
                                }
                            }
                        }
                    }
                }

                if (embed.Length > EMBEDTOTALLENGTH_MAX)
                {
                    return new ArgumentParseResult($"The total length of the embed may not exceed {EMBEDTOTALLENGTH_MAX} characters!");
                }
            }

            return ArgumentParseResult.SuccessfullParse;
        }

        public static void GetJSONFromUserMessage(IMessage message, out JSONContainer json)
        {
            string messageContent = message.Content;
            IEmbed embed = null;

            IReadOnlyCollection<IEmbed> embeds = message.Embeds;

            if ((embeds != null) && embeds.Count > 0)
            {
                foreach (IEmbed iembed in embeds)
                {
                    embed = iembed;
                    break;
                }
            }

            GetJSONFromMessageContentAndEmbed(messageContent, embed, out json);
        }

        public static void GetJSONFromMessageContentAndEmbed(string messageContent, IEmbed embed, out JSONContainer json)
        {
            json = JSONContainer.NewObject();

            if (messageContent != null)
            {
                json.TryAddField(MESSAGECONTENT, messageContent);
            }

            if (embed != null)
            {
                JSONContainer embedJSON = JSONContainer.NewObject();

                // Insert TITLE, DESCRIPTION, TITLE_URL, TIMESTAMP

                if (!string.IsNullOrEmpty(embed.Title))
                {
                    embedJSON.TryAddField(TITLE, embed.Title);
                }
                if (!string.IsNullOrEmpty(embed.Description))
                {
                    embedJSON.TryAddField(DESCRIPTION, embed.Description);
                }
                if (!string.IsNullOrEmpty(embed.Url))
                {
                    embedJSON.TryAddField(URL, embed.Url);
                }
                if (embed.Timestamp != null)
                {
                    embedJSON.TryAddField(TIMESTAMP, embed.Timestamp?.ToString("u"));
                }

                // Insert AUTHOR

                if (embed.Author != null)
                {
                    EmbedAuthor author = embed.Author.Value;
                    JSONContainer authorJSON = JSONContainer.NewObject();

                    if (!string.IsNullOrEmpty(author.Name))
                    {
                        authorJSON.TryAddField(NAME, author.Name);
                    }
                    if (!string.IsNullOrEmpty(author.IconUrl))
                    {
                        authorJSON.TryAddField(ICON_URL, author.IconUrl);
                    }
                    if (!string.IsNullOrEmpty(author.Url))
                    {
                        authorJSON.TryAddField(URL, author.Url);
                    }

                    embedJSON.TryAddField(AUTHOR, authorJSON);
                }

                // Insert THUMBNAIL, IMAGE

                if (embed.Thumbnail != null)
                {
                    if (!string.IsNullOrEmpty(embed.Thumbnail.Value.Url))
                    {
                        JSONContainer thumbnailJSON = JSONContainer.NewObject();
                        thumbnailJSON.TryAddField(URL, embed.Thumbnail.Value.Url);
                        embedJSON.TryAddField(THUMBNAIL, thumbnailJSON);
                    }
                }
                if (embed.Image != null)
                {
                    if (!string.IsNullOrEmpty(embed.Image.Value.Url))
                    {
                        JSONContainer imagJSON = JSONContainer.NewObject();
                        imagJSON.TryAddField(URL, embed.Image.Value.Url);
                        embedJSON.TryAddField(IMAGE, imagJSON);
                    }
                }

                // Insert Color

                if (embed.Color != null)
                {
                    if (embed.Color.Value.RawValue != 0)
                    {
                        embedJSON.TryAddField(COLOR, embed.Color.Value.RawValue);
                    }
                }

                // Insert Footer

                if (embed.Footer != null)
                {
                    EmbedFooter footer = embed.Footer.Value;
                    JSONContainer footerJSON = JSONContainer.NewObject();

                    if (!string.IsNullOrEmpty(footer.Text))
                    {
                        footerJSON.TryAddField(TEXT, footer.Text);
                    }
                    if (!string.IsNullOrEmpty(footer.IconUrl))
                    {
                        footerJSON.TryAddField(ICON_URL, footer.IconUrl);
                    }

                    embedJSON.TryAddField(FOOTER, footerJSON);
                }

                // Insert Fields

                if ((embed.Fields != null) && embed.Fields.Length > 0)
                {
                    JSONContainer fieldsJSON = JSONContainer.NewArray();

                    foreach (Discord.EmbedField embedField in embed.Fields)
                    {
                        JSONContainer fieldJSON = JSONContainer.NewObject();
                        fieldJSON.TryAddField(NAME, embedField.Name);
                        fieldJSON.TryAddField(VALUE, embedField.Value);
                        fieldJSON.TryAddField(INLINE, embedField.Inline);
                        fieldsJSON.Add(fieldJSON);
                    }

                    embedJSON.TryAddField(FIELDS, fieldsJSON);
                }

                json.TryAddField(EMBED, embedJSON);
            }
        }
    }
}
