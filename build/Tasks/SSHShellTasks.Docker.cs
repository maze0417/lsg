using Renci.SshNet;

namespace Builds.Deployment.Tasks
{
    //No sudo for remote shell , refer : https://github.com/sindresorhus/guides/blob/master/docker-without-sudo.md 

    public partial class SshShellTasks
    {
        public void RemoveImage(string imageName)
        {
            if (!IsImageExisted(imageName)) return;
            var cmd = $"docker image rm {imageName} -f ";
            ExecuteScript(cmd);
        }


        public void CleanupImage(string imageName, string ip)
        {
            //clean 10 day before image
            var cmd = @"docker image prune -a --force --filter ""until = 240h""";
            ExecuteScript(cmd, ip);
        }

        public void CleanupImage(string imageName, SshClient client)
        {
            //clean 10 day before image
            var cmd = @"docker image prune -a --force --filter ""until = 240h""";
            ExecuteScript(cmd, client);
        }

        private bool IsImageExisted(string imageName)
        {
            var response = GetImageByName(imageName);
            return response?.Length > 0;
        }

        public bool IsImageExisted(string imageName, string ip)
        {
            var response = GetImageByName(imageName, ip);
            return response?.Length > 0;
        }

        public bool IsImageExisted(string imageName, SshClient client)
        {
            var response = GetImageByName(imageName, client);
            return response?.Length > 0;
        }

        private string GetImageByName(string imageName, string ip)
        {
            var cmd =
                $"docker image ls --filter 'reference={imageName}' --format '{{{{.Repository}}}}:{{{{.Tag}}}}'";
            return ExecuteScript(cmd, ip);
        }

        private string GetImageByName(string imageName, SshClient client)
        {
            var cmd =
                $"docker image ls --filter 'reference={imageName}' --format '{{{{.Repository}}}}:{{{{.Tag}}}}'";
            return ExecuteScript(cmd, client);
        }

        private string GetImageByName(string imageName)
        {
            var cmd =
                $"docker image ls --filter 'reference={imageName}' --format '{{{{.Repository}}}}:{{{{.Tag}}}}'";
            return ExecuteScript(cmd);
        }

        private bool ContainerExists(string containerName)
        {
            var response = GetContainerByName(containerName);
            return response?.Length > 0;
        }

        public bool ContainerExists(string containerName, string ip)
        {
            var response = GetContainerByName(containerName, ip);
            return response?.Length > 0;
        }

        public bool ContainerExists(string containerName, SshClient client)
        {
            var response = GetContainerByName(containerName, client);
            return response?.Length > 0;
        }

        private string GetContainerByName(string containerName)
        {
            var cmd = $@"docker ps -a -f name={containerName} --format ""{{{{.Names}}}}""";
            return ExecuteScript(cmd);
        }

        private string GetContainerByName(string containerName, string ip)
        {
            var cmd = $@"docker ps -a -f name={containerName} --format ""{{{{.Names}}}}""";
            return ExecuteScript(cmd, ip);
        }

        private string GetContainerByName(string containerName, SshClient client)
        {
            var cmd = $@"docker ps -a -f name={containerName} --format ""{{{{.Names}}}}""";
            return ExecuteScript(cmd, client);
        }

        public void RemoveContainerIfExisted(string containerName)
        {
            if (!ContainerExists(containerName)) return;

            var cmd = $"docker rm {containerName} -f";
            ExecuteScript(cmd);
        }

        public void RemoveAllContainer(string ip)
        {
            var result = ExecuteScript("docker ps -a -q", ip);

            if (result?.Length > 0)
            {
                var cmd = "docker rm $(docker ps -a -q) -f";
                ExecuteScript(cmd, ip);
            }
        }

        public void RemoveAllContainer(SshClient client)
        {
            var result = ExecuteScript("docker ps -a -q", client);

            if (result?.Length > 0)
            {
                var cmd = "docker rm $(docker ps -a -q) -f";
                ExecuteScript(cmd, client);
            }
        }

        public void EnsureLogin()
        {
            var cmd = $"docker login -u ugsrepo -p ugs#1qaz@WSX registry.acsdev.net";
            ExecuteScript(cmd);
        }

        public void EnsureLogin(string ip)
        {
            var cmd = $"docker login -u ugsrepo -p ugs#1qaz@WSX registry.acsdev.net";
            ExecuteScript(cmd, ip);
        }

        public void EnsureLogin(SshClient client)
        {
            var cmd = $"docker login -u ugsrepo -p ugs#1qaz@WSX registry.acsdev.net";
            ExecuteScript(cmd, client);
        }
    }
}