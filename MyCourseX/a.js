$.ajax({
            async: false,
            url: "http://cp.mycourse.cn/wxcourse/addJiaoXueJiHuainfo.action",
            type: "GET",
            dataType: "jsonp",
            data: finishData,
            timeout: 5000
        });