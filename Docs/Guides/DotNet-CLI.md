```shell
dotnet clean
dotnet build `
                                        # 솔루션 파일
    --configuration Release             # 
    /p:TreatWarningsAsErrors=true       # 경고를 오류로 처리
    --no-incremental                    # 증분 빌드 비활성화
    -v:n                                # 로그 출력 수준