﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DRRR.Server.Services;
using DRRR.Server.Security;
using Newtonsoft.Json.Linq;
using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.IO;
using static DRRR.Server.Security.HashidsHelper;
using static System.IO.File;
using DRRR.Server.Utils;
using DRRR.Server.Models;
using System.Linq;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace DRRR.Server.Controllers
{
    /// <summary>
    /// 资源控制器
    /// </summary>
    [Route("api/[controller]")]
    public class ResourcesController : Controller
    {
        private readonly string _picturesDirectory;

        private readonly ImageService _imageService;

        public ResourcesController(
            SystemMessagesService systemMessagesService,
            ImageService imageService,
            IConfiguration configuration)
        {
            _imageService = imageService;
            _picturesDirectory = configuration["Resources:Pictures"];
        }

        /// <summary>
        /// 获取客户端的系统通知信息配置
        /// </summary>
        /// <returns>表示获取客户端的系统通知信息配置的任务</returns>
        [HttpGet("system-messages")]
        public async Task<JsonResult> GetSystemMessageSettingsAsync()
        {
            var json = await ReadAllTextAsync(
                Path.Combine(AppContext.BaseDirectory, "Resources", "system-messages.client.json"));
            return Json(JObject.Parse(json));
        }

        /// <summary>
        /// 获取用户头像资源
        /// </summary>
        /// <param name="type">图片类型（原图或者缩略图）</param>
        /// <param name="hashid">用户哈希ID</param>
        /// <param name="userProfileService">用户信息管理服务</param>
        /// <returns>异步获取用户头像资源的任务</returns>
        [HttpGet("avatars/{type}/{hashid}")]
        public async Task<FileResult> GetAvatarAsync(string type, string hashid,
            [FromServices]UserProfileService userProfileService)
            => await userProfileService.GetAvatarAsync(type, Decode(hashid));

        /// <summary>
        /// 更新用户头像资源
        /// </summary>
        /// <param name="hashid">用户哈希ID</param>
        /// <param name="avatar">用户头像文件</param>
        /// <param name="userProfileService">用户信息管理服务</param>
        /// <returns>表示用户更新用户头像资源的任务</returns>
        [HttpPut("avatars/{hashid}")]
        [JwtAuthorize(Roles.User, Roles.Admin)]
        public async Task<bool> UpdateAvatarAsync(string hashid, [FromForm]IFormFile avatar,
            [FromServices]UserProfileService userProfileService)
            => await userProfileService.UpdateAvatarAsync(Decode(hashid), avatar);

        /// <summary>
        /// 获取聊天图片
        /// </summary>
        /// <param name="roomHashid">房间ID</param>
        /// <param name="userHashid">用户ID</param>
        /// <param name="timestamp">图片被发送的时间戳</param>
        /// <param name="dbContext">DB上下文</param>
        /// <returns>表示异步获取图片的任务</returns>
        [HttpGet("chat-pictures/rooms/{roomHashid}/users/{userHashid}")]
        [JwtAuthorize(Roles.Guest, Roles.User, Roles.Admin)]
        public async Task<ActionResult> GetChatPictureAsync(
            string roomHashid,
            string userHashid,
            string timestamp,
            [FromServices]DrrrDbContext dbContext)
        {
            int roomId = Decode(roomHashid);
            int userId = Decode(userHashid);
            int currentUserId = Decode(User.FindFirst("uid").Value);
            // 判断当前用户是否属于该聊天室
            if (!dbContext.Connection.Any(c => c.RoomId == roomId && c.UserId == currentUserId))
            {
                return Forbid(JwtBearerDefaults.AuthenticationScheme);
            }
            string fileFullNameWithoutExtension = Path.Combine(_picturesDirectory,
                roomId.ToString(), "originals", $"{userId}_{timestamp}");
            string path;
            if (Exists(path = $"{fileFullNameWithoutExtension}.jpg")
                || Exists(path = $"{fileFullNameWithoutExtension}.gif"))
            {
                return File(await ReadAllBytesAsync(path), _imageService.GetMimeType(path));
            }
            return NotFound();
        }
    }
}
