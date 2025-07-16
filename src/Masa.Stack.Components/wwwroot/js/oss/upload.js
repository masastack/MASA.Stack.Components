async function UploadImage(imageFiles, ossParamter) {
    const client = new OSS(ossParamter);
    let cdnHost = getCdnHost(ossParamter);
    const headers = {
        // ָ����Object������ʱ��ҳ�Ļ�����Ϊ��
        // 'Cache-Control': 'no-cache',
        // ָ����Object������ʱ�����ơ�
        // 'Content-Disposition': 'oss_download.txt',
        // ָ����Object������ʱ�����ݱ����ʽ��
        // 'Content-Encoding': 'UTF-8',
        // ָ������ʱ�䡣
        // 'Expires': 'Wed, 08 Jul 2022 16:57:01 GMT',
        // ָ��Object�Ĵ洢���͡�
        // 'x-oss-storage-class': 'Standard',
        // ָ��Object�ķ���Ȩ�ޡ�
        'x-oss-object-acl': 'public-read-write',
        // ����Object�ı�ǩ����ͬʱ���ö����ǩ��
        // 'x-oss-tagging': 'Tag1=1&Tag2=2',
        // ָ��CopyObject����ʱ�Ƿ񸲸�ͬ��Ŀ��Object���˴�����Ϊtrue����ʾ��ֹ����ͬ��Object��
        // 'x-oss-forbid-overwrite': 'true',
    };
    console.log(ossParamter);
    return await putObject(client, imageFiles[0], headers, cdnHost, ossParamter.randomName);
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

async function putObject(client, file, headers, cdnHost, randName) {
    try {
        let fileName = getFileName(file, randName);
        // ��дObject����·����Object����·���в��ܰ���Bucket���ơ�
        // ������ͨ���Զ����ļ���������exampleobject.txt�����ļ�����·��������exampledir/exampleobject.txt������ʽʵ�ֽ������ϴ�����ǰBucket��Bucket�е�ָ��Ŀ¼��
        // data��������Զ���Ϊfile����Blob���ݻ���OSS Buffer��
        const result = await client.put(
            `${fileName}`,
            file,
            {
                headers
            }
        );
        console.log(result);
        let url = cdnHost ? (cdnHost + "/" + result.name) : result.url;
        console.log(url)
        return [url];
    } catch (e) {
        console.log(e);
    }
}