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
    return await putObject(client, imageFiles[0], headers, cdnHost);
}

function getCdnHost(ossParamter) {
    let cdnHost = ((ossParamter || {}).cdnHost || "").trim();
    if (cdnHost.length == 0) {
        return cdnHost;
    }
    if (cdnHost[cdnHost.length - 1] == '/')
        return cdnHost.substring(0, cdnHost.length - 1);
}

async function putObject(client, file, headers, cdnHost) {
    try {
        // ��дObject����·����Object����·���в��ܰ���Bucket���ơ�
        // ������ͨ���Զ����ļ���������exampleobject.txt�����ļ�����·��������exampledir/exampleobject.txt������ʽʵ�ֽ������ϴ�����ǰBucket��Bucket�е�ָ��Ŀ¼��
        // data��������Զ���Ϊfile����Blob���ݻ���OSS Buffer��
        const result = await client.put(
            `${file.name}`,
            file,
            {
                headers
            }
        );
        console.log(result);
        console.log("cdnHost: " + cdnHost);
        let url = cdnHost ? (cdnHost + "/" + result.name) : result.url;
        console.log(url)
        return [url];
    } catch (e) {
        console.log(e);
    }
}