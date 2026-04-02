<template>
  <div class="fund-list-container">
    <el-card class="fund-list-header">
      <template #header>
        <div class="card-header">
          <span>基金列表</span>
        </div>
      </template>
      
      <!-- 筛选条件 -->
      <div class="filter-section">
        <el-row :gutter="20">
          <el-col :span="8">
            <el-form-item label="基金类型">
              <el-select v-model="filter.fundType" placeholder="请选择基金类型" clearable>
                <el-option label="混合型" value="混合型" />
                <el-option label="股票型" value="股票型" />
                <el-option label="债券型" value="债券型" />
                <el-option label="货币型" value="货币型" />
                <el-option label="指数型" value="指数型" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="风险等级">
              <el-select v-model="filter.riskLevel" placeholder="请选择风险等级" clearable>
                <el-option label="低风险" value="低风险" />
                <el-option label="中低风险" value="中低风险" />
                <el-option label="中风险" value="中风险" />
                <el-option label="中高风险" value="中高风险" />
                <el-option label="高风险" value="高风险" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="搜索">
              <el-input v-model="filter.keyword" placeholder="输入基金代码或名称" clearable />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row>
          <el-col :span="24" style="text-align: right;">
            <el-button type="primary" @click="handleSearch">查询</el-button>
            <el-button @click="resetFilter">重置</el-button>
          </el-col>
        </el-row>
      </div>
    </el-card>

    <!-- 基金列表 -->
    <el-card class="fund-list-content">
      <el-table :data="funds" style="width: 100%" v-loading="loading">
        <el-table-column prop="code" label="基金代码" width="100" />
        <el-table-column prop="name" label="基金名称" min-width="180" />
        <el-table-column prop="fundType" label="基金类型" width="100" />
        <el-table-column prop="manager" label="基金经理" width="120" />
        <el-table-column prop="establishDate" label="成立日期" width="120" />
        <el-table-column prop="riskLevel" label="风险等级" width="100" />
        <el-table-column label="操作" width="150">
          <template #default="scope">
            <el-button type="primary" size="small" @click="goToFundDetail(scope.row.code)">详情</el-button>
            <el-button type="success" size="small" @click="addToFavorites(scope.row.code)">收藏</el-button>
          </template>
        </el-table-column>
      </el-table>

      <!-- 分页 -->
      <div class="pagination-section">
        <el-pagination
          v-model:current-page="pagination.current"
          v-model:page-size="pagination.pageSize"
          :page-sizes="[10, 20, 50, 100]"
          layout="total, sizes, prev, pager, next, jumper"
          :total="pagination.total"
          @size-change="handleSizeChange"
          @current-change="handleCurrentChange"
        />
      </div>
    </el-card>
  </div>
</template>

<script setup>
import { ref, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { useFundStore } from '../store/fundStore'

const router = useRouter()
const fundStore = useFundStore()

// 筛选条件
const filter = ref({
  fundType: '',
  riskLevel: '',
  keyword: ''
})

// 分页信息
const pagination = ref({
  current: 1,
  pageSize: 10,
  total: 0
})

// 基金列表
const funds = ref([])
const loading = ref(false)

// 跳转到基金详情页面
const goToFundDetail = (code) => {
  router.push(`/fund/${code}`)
}

// 添加到自选基金
const addToFavorites = (code) => {
  fundStore.addFavorite(code)
  ElMessage.success('添加成功')
}

// 处理搜索
const handleSearch = () => {
  pagination.value.current = 1
  fetchFunds()
}

// 重置筛选条件
const resetFilter = () => {
  filter.value = {
    fundType: '',
    riskLevel: '',
    keyword: ''
  }
  pagination.value.current = 1
  fetchFunds()
}

// 分页大小变化
const handleSizeChange = (size) => {
  pagination.value.pageSize = size
  fetchFunds()
}

// 当前页变化
const handleCurrentChange = (current) => {
  pagination.value.current = current
  fetchFunds()
}

// 获取基金列表
const fetchFunds = async () => {
  loading.value = true
  try {
    const response = await fundStore.getFundList(
      pagination.value.current,
      pagination.value.pageSize,
      filter.value.fundType,
      filter.value.riskLevel
    )
    funds.value = response.funds
    pagination.value.total = response.total
  } catch (error) {
    console.error('获取基金列表失败:', error)
    ElMessage.error('获取基金列表失败')
  } finally {
    loading.value = false
  }
}

// 初始化
onMounted(() => {
  fetchFunds()
})

// 监听筛选条件变化
watch(filter, () => {
  // 可以在这里添加防抖处理
}, { deep: true })
</script>

<style scoped>
.fund-list-container {
  padding: 20px;
  max-width: 1200px;
  margin: 0 auto;
}

.fund-list-header,
.fund-list-content {
  margin-bottom: 20px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.filter-section {
  padding: 20px 0;
}

.pagination-section {
  margin-top: 20px;
  display: flex;
  justify-content: flex-end;
}

@media (max-width: 768px) {
  .fund-list-container {
    padding: 10px;
  }
  
  .el-table {
    font-size: 12px;
  }
  
  .el-table th,
  .el-table td {
    padding: 8px;
  }
}
</style>
