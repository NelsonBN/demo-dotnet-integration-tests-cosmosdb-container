# Demo - CosmosDb Integration Tests with .NET and TestContainers


## Demos

### Init Database and container with Docker-Compose

- [Docker-Compose](./docker-compose.init.yml)
- [Init Script](./tests/init-db.py)


## CosmosDb Emulator - Details

### Health Checks Endpoints

- `GET http://localhost:8080/alive`: returns 503 if PostgreSQL and Gateway are unhealthy
- `GET http://localhost:8080/ready`: returns 503 if any component is not in the right state (if explorer is disabled, that doesn't count)
- `GET http://localhost:8080/status`: always return 200 and you can parse the response body in JSON for details
> Reference: [Endpoints source](https://github.com/Azure/azure-cosmos-db-emulator-docker/issues/209#issuecomment-3487701068)




## References

- [GitHub Repository](https://github.com/Azure/azure-cosmos-db-emulator-docker)
- [Official Doc](https://learn.microsoft.com/en-gb/azure/cosmos-db/emulator-linux)
- [Emulator - Commands](https://learn.microsoft.com/en-gb/azure/cosmos-db/emulator-linux#docker-commands)
- [Emulator - Feature support](https://learn.microsoft.com/en-gb/azure/cosmos-db/emulator-linux#feature-support)
- [Emulator - Limitations](https://learn.microsoft.com/en-gb/azure/cosmos-db/emulator-linux#limitations)
