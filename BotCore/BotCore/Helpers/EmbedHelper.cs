using BotCoreNET.CommandHandling;
using Discord;
using JSON;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BotCoreNET.Helpers
{
    public static class EmbedHelper
    {
        #region Constants

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

        #endregion
        #region Parsing Message from JSON

        /// <summary>
        /// Retrieves messageContent and embed from JSON. Throws <see cref="EmbedParseException"/> if invalid formatting is found.
        /// </summary>
        /// <param name="json">JSONContainer object to parse from</param>
        /// <param name="embed">embed result. Null if not specified in <paramref name="json"/></param>
        /// <param name="messageContent">Message content result. Null if not specified in <paramref name="json"/></param>
        public static void GetMessageFromJSONObject(JSONContainer json, out EmbedBuilder embed, out string messageContent)
        {
            messageContent = getLengthCheckedStringField(json, MESSAGECONTENT, MESSAGECONTENT_MAX, "The message content may not exceed {0} characters in length!");
            if (json.TryGetObjectField(EMBED, out JSONContainer embedJSON))
            {
                embed = getEmbed(embedJSON);
            }
            else
            {
                embed = null;
            }

            // Sanity check for errors

            if (string.IsNullOrEmpty(messageContent) && embedJSON == null)
            {
                throw new EmbedParseException("Message is empty, neither embed nor messagecontent were specified!");
            }
        }

        /// <summary>
        /// Attempts to construct messageContent and embed from JSON
        /// </summary>
        /// <remarks>
        /// Use only when rarely called and parsing errors are expected (ex.: User Input)! Literally a try-catch block around <see cref="GetMessageFromJSONObject(JSONContainer, out EmbedBuilder, out string)"/>!
        /// </remarks>
        /// <param name="json">JSON to read from</param>
        /// <param name="embed">Embed result. Null if not specified</param>
        /// <param name="messageContent">Content string result. Null if not specified</param>
        /// <param name="error">Parsing error if one occurs. Null otherwise</param>
        /// <returns>True, if no errors were thrown</returns>
        public static bool TryGetMessageFromJSONObject(JSONContainer json, out EmbedBuilder embed, out string messageContent, out string error)
        {
            try
            {
                GetMessageFromJSONObject(json, out embed, out messageContent);
                error = null;
                return true;
            }
            catch (EmbedParseException e)
            {
                embed = null;
                messageContent = null;
                error = e.Message;
                return false;
            }
        }

        private static EmbedBuilder getEmbed(JSONContainer embedJSON)
        {
            EmbedBuilder embed = new EmbedBuilder
            {
                Title = getLengthCheckedStringField(embedJSON, TITLE, EMBEDTITLE_MAX, "The embed title may not exceed {0} characters!"),
                Description = getLengthCheckedStringField(embedJSON, DESCRIPTION, EMBEDDESCRIPTION_MAX, "The embed description may not exceed {0} characters!"),
                Url = getFormCheckedURLField(embedJSON, URL, "The embed URL is not a well formed URL!"),
                Timestamp = getParsedTimestampField(embedJSON, TIMESTAMP, "Could not parse the timestamp to a DateTimeOffset"),
                ThumbnailUrl = getFormCheckedImageField(embedJSON, THUMBNAIL, "The URL for the embed thumbnail is not a well formed URL!"),
                ImageUrl = getFormCheckedImageField(embedJSON, IMAGE, "The url for the embed image is not a well formed url!")
            };

            if (embedJSON.TryGetField(AUTHOR, out JSONContainer authorJSON))
            {
                embed.Author = getAuthor(authorJSON);
            }

            if (embedJSON.TryGetField(COLOR, out int color))
            {
                embed.Color = new Color((uint)color);
            }

            if (embedJSON.TryGetField(FOOTER, out JSONContainer footerJSON))
            {
                embed.Footer = getFooter(footerJSON);
            }

            if (embedJSON.TryGetField(FIELDS, out IReadOnlyList<JSONField> fieldsJSON))
            {
                embed.Fields = getEmbedFields(fieldsJSON);
            }

            // Length check

            if (embed.Length > EMBEDTOTALLENGTH_MAX)
            {
                throw new EmbedParseException($"The total length of the embed may not exceed {EMBEDTOTALLENGTH_MAX} characters!");
            }

            return embed;
        }

        private static string getLengthCheckedStringField(JSONContainer json, string identifier, int maxlength, string errorstring)
        {
            if (json.TryGetField(identifier, out string str))
            {
                if (!string.IsNullOrEmpty(str))
                {
                    if (str.Length > maxlength)
                    {
                        throw new EmbedParseException(string.Format(errorstring, maxlength));
                    }
                    return str;
                }
            }
            return null;
        }

        private static string getFormCheckedURLField(JSONContainer json, string identifier, string errorstring)
        {
            if (json.TryGetField(identifier, out string embedURL))
            {
                if (!string.IsNullOrEmpty(embedURL))
                {
                    if (Uri.IsWellFormedUriString(embedURL, UriKind.Absolute))
                    {
                        return embedURL;
                    }
                    else
                    {
                        throw new EmbedParseException(errorstring);
                    }
                }
            }
            return null;
        }

        private static string getFormCheckedImageField(JSONContainer json, string identifier, string errorstring)
        {
            if (json.TryGetObjectField(identifier, out JSONContainer imageJSON))
            {
                return getFormCheckedURLField(imageJSON, URL, errorstring);
            }
            return null;
        }

        private static DateTimeOffset? getParsedTimestampField(JSONContainer json, string identifier, string error)
        {
            if (json.TryGetField(identifier, out string timestamp_str))
            {
                if (!string.IsNullOrEmpty(timestamp_str))
                {
                    if (DateTimeOffset.TryParse(timestamp_str, out DateTimeOffset timestamp))
                    {
                        return timestamp;
                    }
                    else
                    {
                        throw new EmbedParseException(error);
                    }
                }
            }
            return null;
        }

        private static EmbedAuthorBuilder getAuthor(JSONContainer authorJSON)
        {
            return new EmbedAuthorBuilder
            {
                Name = getLengthCheckedStringField(authorJSON, NAME, EMBEDAUTHORNAME_MAX, "The embed author name may not exceed {0} characters!"),
                IconUrl = getFormCheckedURLField(authorJSON, ICON_URL, "The URL for the author icon is not a well formed URL"),
                Url = getFormCheckedURLField(authorJSON, URL, "The URL for the author link is not a well formed URL")
            };
        }

        private static EmbedFooterBuilder getFooter(JSONContainer footerJSON)
        {
            return new EmbedFooterBuilder()
            {
                Text = getLengthCheckedStringField(footerJSON, TEXT, EMBEDFOOTERTEXT_MAX, "The embed footer text may not exceed {0} characters!"),
                IconUrl = getFormCheckedURLField(footerJSON, ICON_URL, "The url for the embed footer icon is not a well formed url!")
            };
        }

        private static List<EmbedFieldBuilder> getEmbedFields(IReadOnlyList<JSONField> fieldsJSON)
        {
            List<EmbedFieldBuilder> fields = new List<EmbedFieldBuilder>(fieldsJSON.Count);
            if (fieldsJSON.Count > EMBEDFIELDCOUNT_MAX)
            {
                throw new EmbedParseException($"The embed can not have more than {EMBEDFIELDCOUNT_MAX} fields!");
            }
            foreach (JSONField fieldJSON in fieldsJSON)
            {
                if (fieldJSON.IsObject)
                {
                    if (fieldJSON.Container.TryGetField(NAME, out string name) && fieldJSON.Container.TryGetField(VALUE, out string value))
                    {
                        fieldJSON.Container.TryGetField(INLINE, out bool isInline, false);
                        if (name != null && value != null)
                        {
                            if (name.Length > EMBEDFIELDNAME_MAX)
                            {
                                throw new EmbedParseException($"A field name may not exceed {EMBEDFIELDNAME_MAX} characters!");
                            }
                            if (value.Length > EMBEDFIELDVALUE_MAX)
                            {
                                throw new EmbedParseException($"A field value may not exceed {EMBEDFIELDVALUE_MAX} characters!");
                            }
                            fields.Add(new EmbedFieldBuilder() { Name = name, Value = value, IsInline = isInline });
                        }
                    }
                }
            }
            return fields;
        }

        #endregion
        #region Forming JSON from Messages

        /// <summary>
        /// Constructs a <see cref="JSONContainer"/> from a Message
        /// </summary>
        /// <param name="message">Message to pull information from</param>
        public static JSONContainer GetJSONFromUserMessage(IMessage message)
        {
            string messageContent = message.Content;
            IEmbed embed = null;

            IReadOnlyCollection<IEmbed> embeds = message.Embeds;

            if (embeds != null)
            {
                embed = embeds.FirstOrDefault();
            }

            return GetJSONFromMessageContentAndEmbed(messageContent, embed);
        }

        /// <summary>
        /// Constructs a <see cref="JSONContainer"/> from a content string and an embed
        /// </summary>
        /// <param name="messageContent">Message Content of the message</param>
        /// <param name="embed">Embed of the message</param>
        public static JSONContainer GetJSONFromMessageContentAndEmbed(string messageContent, IEmbed embed)
        {
            JSONContainer json = JSONContainer.NewObject();

            checkAddStringField(messageContent, json, MESSAGECONTENT);

            if (embed != null)
            {
                JSONContainer embedJSON = getEmbedJSON(embed);
                json.TryAddField(EMBED, embedJSON);
            }
            return json;
        }

        private static JSONContainer getEmbedJSON(IEmbed embed)
        {
            JSONContainer embedJSON = JSONContainer.NewObject();

            // Insert TITLE, DESCRIPTION, TITLE_URL, TIMESTAMP

            checkAddStringField(embed.Title, embedJSON, TITLE);
            checkAddStringField(embed.Description, embedJSON, DESCRIPTION);
            checkAddStringField(embed.Url, embedJSON, URL);

            if (embed.Timestamp != null)
            {
                embedJSON.TryAddField(TIMESTAMP, embed.Timestamp?.ToString("u"));
            }

            // Insert AUTHOR

            if (embed.Author != null)
            {
                JSONContainer authorJSON = getAuthor(embed);

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
                embedJSON.TryAddField(COLOR, embed.Color.Value.RawValue);
            }

            // Insert Footer

            if (embed.Footer != null)
            {
                JSONContainer footerJSON = getFooterJSON(embed);

                embedJSON.TryAddField(FOOTER, footerJSON);
            }

            // Insert Fields

            if ((embed.Fields != null) && embed.Fields.Length > 0)
            {
                JSONContainer fieldsJSON = getFieldsJSON(embed);

                embedJSON.TryAddField(FIELDS, fieldsJSON);
            }

            return embedJSON;
        }

        private static JSONContainer getAuthor(IEmbed embed)
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

            return authorJSON;
        }

        private static JSONContainer getFooterJSON(IEmbed embed)
        {
            EmbedFooter footer = embed.Footer.Value;
            JSONContainer footerJSON = JSONContainer.NewObject();

            checkAddStringField(footer.Text, footerJSON, TEXT);
            checkAddStringField(footer.IconUrl, footerJSON, ICON_URL);

            return footerJSON;
        }

        private static JSONContainer getFieldsJSON(IEmbed embed)
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

            return fieldsJSON;
        }

        private static void checkAddStringField(string value, JSONContainer json, string identifier)
        {
            if (!string.IsNullOrEmpty(value))
            {
                json.TryAddField(identifier, value);
            }
        }

        #endregion
    }
}
