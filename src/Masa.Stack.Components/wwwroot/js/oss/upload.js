async function UploadImage(imageFiles, ossParamter) {
    const client = new OSS(ossParamter);
    let cdnHost = getCdnHost(ossParamter);
    const headers = {
        // 指定该Object被下载时网页的缓存行为。
        // 'Cache-Control': 'no-cache',
        // 指定该Object被下载时的名称。
        // 'Content-Disposition': 'oss_download.txt',
        // 指定该Object被下载时的内容编码格式。
        // 'Content-Encoding': 'UTF-8',
        // 指定过期时间。
        // 'Expires': 'Wed, 08 Jul 2022 16:57:01 GMT',
        // 指定Object的存储类型。
        // 'x-oss-storage-class': 'Standard',
        // 指定Object的访问权限。
        'x-oss-object-acl': 'public-read-write',
        // 设置Object的标签，可同时设置多个标签。
        // 'x-oss-tagging': 'Tag1=1&Tag2=2',
        // 指定CopyObject操作时是否覆盖同名目标Object。此处设置为true，表示禁止覆盖同名Object。
        // 'x-oss-forbid-overwrite': 'true',
    };
    console.log(ossParamter);
    return await putObject(client, imageFiles[0], headers, cdnHost, ossParamter.randomName, ossParamter.rootDirectory);
}

function getCdnHost(ossParamter) {
    let cdnHost = (ossParamter.cdnHost || "").trim();
    if (cdnHost.length == 0) {
        return cdnHost;
    }
    if (cdnHost[cdnHost.length - 1] == '/')
        return cdnHost.substring(0, cdnHost.length - 1);
}

function getFileName(file, rand) {
    if (!rand) return file.name;
    let time = formatTime(new Date()), random = Math.random().toString().slice(3, 9);
    let lastPosition = file.name.lastIndexOf('.');
    let ext = lastPosition >= 0 ? file.name.slice(lastPosition) : '';
    return `${time}_${random}${ext}`;
}

function formatTime(date) {
    const year = date.getFullYear();
    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    const day = date.getDate().toString().padStart(2, '0');
    const hour = date.getHours().toString().padStart(2, '0');
    const minute = date.getMinutes().toString().padStart(2, '0');
    const second = date.getSeconds().toString().padStart(2, '0');
    const offset = -date.getTimezoneOffset();
    const sign = offset >= 0 ? 1 : 0;
    return `${year}${month}${day}${hour}${minute}${second}_${sign}${offset}`;
}

async function putObject(client, file, headers, cdnHost, randName, rootDirectory) {
    try {
        if (!rootDirectory) rootDirectory = '/';
        else rootDirectory = rootDirectory.trim();
        if (rootDirectory == '') rootDirectory = '/';
        else if (rootDirectory[0] != '/') rootDirectory = '/' + rootDirectory;
        if (rootDirectory[rootDirectory.length - 1] != '/') rootDirectory = rootDirectory + '/';
        let fileName = `${rootDirectory}${getFileName(file, randName)}`;
        // 填写Object完整路径。Object完整路径中不能包含Bucket名称。
        // 您可以通过自定义文件名（例如exampleobject.txt）或文件完整路径（例如exampledir/exampleobject.txt）的形式实现将数据上传到当前Bucket或Bucket中的指定目录。
        // data对象可以自定义为file对象、Blob数据或者OSS Buffer。
        const result = await client.put(
            `${fileName}`,
            file,
            {
                headers
            }
        );
        console.log(result);
        let url = cdnHost ? (cdnHost + fileName) : result.url;
        console.log(url)
        return [url];
    } catch (e) {
        console.log(e);
    }
}